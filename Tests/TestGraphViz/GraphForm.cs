﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using Renderer;
using SharpMath;
using Nuaj.Cirrus.Utility;
using Nuaj.Cirrus;

namespace TestGraphViz
{
	public partial class GraphForm : Form
	{
		#region CONSTANTS

		#endregion

		#region NESTED TYPES

		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		private struct CB_Main {
			public uint			_nodesCount;
			public uint			_resX;
			public uint			_resY;
		}

		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		private struct CB_Simulation {
			public float		_deltaTime;
			public float		_springConstant;
			public float		_dampingConstant;
		}

		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		private struct SB_NodeSim {
			public float2		m_position;
			public float2		m_velocity;
		}

		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		private struct SB_NodeInfo {
			public float		m_mass;
			public uint			m_linkOffset;
			public uint			m_linksCount;
		}

		#endregion

		#region FIELDS

		Device		m_device = new Device();

		private ConstantBuffer<CB_Main>			m_CB_Main = null;
		private ConstantBuffer<CB_Simulation>	m_CB_Simulation = null;

		private ComputeShader			m_shader_ComputeForces = null;
		private ComputeShader			m_shader_Simulate = null;
		private Shader					m_shader_RenderGraph = null;

		private StructuredBuffer<SB_NodeInfo>	m_SB_Nodes = null;
		private StructuredBuffer<uint>			m_SB_Links = null;
		private StructuredBuffer<float2>		m_SB_Forces = null;
		private StructuredBuffer<SB_NodeSim>[]	m_SB_NodeSims = new StructuredBuffer<SB_NodeSim>[2];

// 		private Texture2D				m_tex_HeatMap_Staging = null;
// 		private Texture2D				m_tex_HeatMap0 = null;
// 		private Texture2D				m_tex_HeatMap1 = null;
// 		private Texture2D				m_tex_Obstacles_Staging = null;
// 		private Texture2D				m_tex_Obstacles0 = null;
// 		private Texture2D				m_tex_Obstacles1 = null;
// 
// 		private Texture2D				m_tex_Search = null;
// 		private Texture2D				m_tex_Search_Staging = null;
// 
// 		private Texture2D				m_tex_FalseColors0 = null;
// 		private Texture2D				m_tex_FalseColors1 = null;

		private ProtoParser.Graph		m_graph = null;
		private uint					m_nodesCount = 0;

		#endregion

		#region METHODS

		public GraphForm() {
			InitializeComponent();
		}

		protected override void OnLoad( EventArgs e ) {
			base.OnLoad( e );

			try {
				m_device.Init( panelOutput.Handle, false, true );
			}
			catch ( Exception _e ) {
				m_device = null;
				MessageBox.Show( "Failed to initialize DX device!\n\n" + _e.Message, "Heat Wave Test", MessageBoxButtons.OK, MessageBoxIcon.Error );
				return;
			}

			//////////////////////////////////////////////////////////////////////////
			// Load the graph we need to simulate
			m_graph = new ProtoParser.Graph();

			using ( FileStream S = new FileInfo( "./Graphs/TestGraph.graph" ).OpenRead() )
				using ( BinaryReader R = new BinaryReader( S ) )
					m_graph.Read( R );

			ProtoParser.Neuron[]	neurons = m_graph.Neurons;
			m_nodesCount = (uint) neurons.Length;


			//////////////////////////////////////////////////////////////////////////
			m_CB_Main = new ConstantBuffer<CB_Main>( m_device, 0 );
			m_CB_Simulation = new ConstantBuffer<CB_Simulation>( m_device, 1 );

			m_CB_Main.m._nodesCount = m_nodesCount;
			m_CB_Main.m._resX = (uint) panelOutput.Width;
			m_CB_Main.m._resY = (uint) panelOutput.Height;
			m_CB_Main.UpdateData();

			m_shader_ComputeForces = new ComputeShader( m_device, new FileInfo( "./Shaders/SimulateGraph.hlsl" ), "CS", null );
			m_shader_Simulate = new ComputeShader( m_device, new FileInfo( "./Shaders/SimulateGraph.hlsl" ), "CS2", null );
			m_shader_RenderGraph = new Shader( m_device, new FileInfo( "./Shaders/RenderGraph.hlsl" ), VERTEX_FORMAT.Pt4, "VS", null, "PS", null );

			// Build node info
			m_SB_Nodes = new StructuredBuffer<SB_NodeInfo>( m_device, m_nodesCount, true );

			Dictionary< ProtoParser.Neuron, uint >	neuron2ID = new Dictionary<ProtoParser.Neuron, uint>( neurons.Length );
			uint	totalLinksCount = 0;
			for ( int neuronIndex=0; neuronIndex < m_nodesCount; neuronIndex++ ) {
				ProtoParser.Neuron	N = neurons[neuronIndex];
				neuron2ID[N] = (uint) neuronIndex;

				uint	linksCount = (uint) (N.ParentsCount + N.ChildrenCount + N.FeaturesCount);
				m_SB_Nodes.m[neuronIndex].m_mass = 1 + linksCount;
				m_SB_Nodes.m[neuronIndex].m_linkOffset = totalLinksCount;
				m_SB_Nodes.m[neuronIndex].m_linksCount = linksCount;

				totalLinksCount += linksCount;
			}
			m_SB_Nodes.Write();

			// Build node links
			m_SB_Links = new StructuredBuffer<uint>( m_device, totalLinksCount, true );
			totalLinksCount = 0;
			for ( int neuronIndex=0; neuronIndex < m_nodesCount; neuronIndex++ ) {
				ProtoParser.Neuron	N = neurons[neuronIndex];
				foreach ( ProtoParser.Neuron O in N.Parents )
					m_SB_Links.m[totalLinksCount++] = neuron2ID[O];
				foreach ( ProtoParser.Neuron O in N.Children )
					m_SB_Links.m[totalLinksCount++] = neuron2ID[O];
				foreach ( ProtoParser.Neuron O in N.Features )
					m_SB_Links.m[totalLinksCount++] = neuron2ID[O];
			}
			m_SB_Links.Write();

			// Initialize sim buffers
			m_SB_Forces = new StructuredBuffer<float2>( m_device, m_nodesCount * m_nodesCount, false );
			m_SB_NodeSims[0] = new StructuredBuffer<SB_NodeSim>( m_device, m_nodesCount, true );
			m_SB_NodeSims[1] = new StructuredBuffer<SB_NodeSim>( m_device, m_nodesCount, true );

			buttonReset_Click( null, EventArgs.Empty );

//			m_shader_HeatDiffusion = new Shader( m_device, new FileInfo( "./Shaders/HeatDiffusion.hlsl" ), VERTEX_FORMAT.Pt4, "VS", null, "PS", null );

// 			m_tex_Search = new Texture2D( m_device, (uint) GRAPH_SIZE, (uint) GRAPH_SIZE, 1, 1, ImageUtility.PIXEL_FORMAT.RGBA8, ImageUtility.COMPONENT_FORMAT.UNORM, false, false, null );
// 			m_tex_Search_Staging = new Texture2D( m_device, (uint) GRAPH_SIZE, (uint) GRAPH_SIZE, 1, 1, ImageUtility.PIXEL_FORMAT.RGBA8, ImageUtility.COMPONENT_FORMAT.UNORM, true, false, null );

// 			// Load false colors
// 			using ( ImageUtility.ImageFile sourceImage = new ImageUtility.ImageFile( new FileInfo( "../../Images/Gradients/Magma.png" ), ImageUtility.ImageFile.FILE_FORMAT.PNG ) ) {
// 				ImageUtility.ImageFile convertedImage = new ImageUtility.ImageFile();
// 				convertedImage.ConvertFrom( sourceImage, ImageUtility.PIXEL_FORMAT.BGRA8 );
// 				using ( ImageUtility.ImagesMatrix image = new ImageUtility.ImagesMatrix( convertedImage, ImageUtility.ImagesMatrix.IMAGE_TYPE.sRGB ) )
// 					m_tex_FalseColors0 = new Texture2D( m_device, image, ImageUtility.COMPONENT_FORMAT.UNORM_sRGB );
// 			}
// 			using ( ImageUtility.ImageFile sourceImage = new ImageUtility.ImageFile( new FileInfo( "../../Images/Gradients/Viridis.png" ), ImageUtility.ImageFile.FILE_FORMAT.PNG ) ) {
// 				ImageUtility.ImageFile convertedImage = new ImageUtility.ImageFile();
// 				convertedImage.ConvertFrom( sourceImage, ImageUtility.PIXEL_FORMAT.BGRA8 );
// 				using ( ImageUtility.ImagesMatrix image = new ImageUtility.ImagesMatrix( convertedImage, ImageUtility.ImagesMatrix.IMAGE_TYPE.sRGB ) )
// 					m_tex_FalseColors1 = new Texture2D( m_device, image, ImageUtility.COMPONENT_FORMAT.UNORM_sRGB );
// 			}

			Application.Idle += Application_Idle;
		}

		void Application_Idle( object sender, EventArgs e ) {
			if ( m_device == null )
				return;

			m_CB_Simulation.m._deltaTime = floatTrackbarControlDeltaTime.Value;
			m_CB_Simulation.m._springConstant = floatTrackbarControlSpringConstant.Value;
			m_CB_Simulation.m._dampingConstant = floatTrackbarControlDampingConstant.Value;
			m_CB_Simulation.UpdateData();


// 			Point	clientPos = panelOutput.PointToClient( Control.MousePosition );
// 			m_CB_Main.m.mousePosition.Set( GRAPH_SIZE * (float) clientPos.X / panelOutput.Width, GRAPH_SIZE * (float) clientPos.Y / panelOutput.Height );
// 			m_CB_Main.m.mouseButtons = (uint) ((((Control.MouseButtons & MouseButtons.Left) != 0) ? 1 : 0)
// //											| (((Control.MouseButtons & MouseButtons.Middle) != 0) ? 2 : 0)
// 											| (m_plotSource ? 2 : 0)
// 											| (((Control.MouseButtons & MouseButtons.Right) != 0) ? 4 : 0)
// 											| (Control.ModifierKeys == Keys.Shift ? 8 : 0));
// 			m_CB_Main.m.diffusionCoefficient = floatTrackbarControlDiffusionCoefficient.Value;
// 			m_CB_Main.m.flags = (uint) (
// 									  (checkBoxShowSearch.Checked ? 1 : 0)
// 
// 									  // 2 bits to select 4 display modes
// 									| (radioButtonShowNormalizedSpace.Checked ? 2 : 0)
// 									| (radioButtonShowResultsSpace.Checked ? 4 : 0)
// 
// 									| (checkBoxShowLog.Checked ? 8 : 0)
// 								);
// 			m_CB_Main.m.sourceIndex = (uint) integerTrackbarControlSimulationSourceIndex.Value;
// 			m_CB_Main.m.sourcesCount = (uint) m_simulationHotSpots.Count;
// 			m_CB_Main.m.resultsConfinementDistance = floatTrackbarControlResultsSpaceConfinement.Value;

			m_device.SetRenderStates( RASTERIZER_STATE.CULL_NONE, DEPTHSTENCIL_STATE.DISABLED, BLEND_STATE.DISABLED );

// 			//////////////////////////////////////////////////////////////////////////
// 			// Perform simulation
// 			if ( m_shader_DrawObstacles.Use() ) {
// 				m_device.SetRenderTarget( m_tex_Obstacles1, null );
// 				m_tex_Obstacles0.SetPS( 0 );
// 				m_device.RenderFullscreenQuad( m_shader_DrawObstacles );
// 
// 				// Swap
// 				Texture2D	temp = m_tex_Obstacles0;
// 				m_tex_Obstacles0 = m_tex_Obstacles1;
// 				m_tex_Obstacles1 = temp;
// 			}

			//////////////////////////////////////////////////////////////////////////
			// Perform simulation
			if ( checkBoxRun.Checked && m_shader_ComputeForces.Use() ) {

				// Compute forces
				m_SB_Nodes.SetInput( 1 );
				m_SB_Links.SetInput( 2 );

				m_SB_NodeSims[0].SetInput( 0 );		// Input positions & velocities
				m_SB_NodeSims[1].SetOutput( 0 );	// Output positions & velocities

				m_SB_Forces.SetOutput( 1 );			// Simulation forces

				m_shader_ComputeForces.Dispatch( m_nodesCount, 1, 1 );

				// Apply forces
				m_shader_Simulate.Use();
				m_shader_Simulate.Dispatch( m_nodesCount, 1, 1 );

				m_SB_NodeSims[0].RemoveFromLastAssignedSlots();
				m_SB_NodeSims[1].RemoveFromLastAssignedSlotUAV();
				m_SB_Forces.RemoveFromLastAssignedSlotUAV();

				// Swap simulation buffers
				StructuredBuffer<SB_NodeSim>	temp = m_SB_NodeSims[0];
				m_SB_NodeSims[0] = m_SB_NodeSims[1];
				m_SB_NodeSims[1] = temp;
			}


			//////////////////////////////////////////////////////////////////////////
			// Render
			if ( m_shader_RenderGraph.Use() ) {
				m_device.SetRenderTarget( m_device.DefaultTarget, null );

				m_SB_NodeSims[0].SetInput( 0 );

				m_device.RenderFullscreenQuad( m_shader_RenderGraph );
			}

			m_device.Present( false );
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing ) {
			if ( disposing && (components != null) ) {
				components.Dispose();

// 				m_tex_FalseColors1.Dispose();
// 				m_tex_FalseColors0.Dispose();

				m_SB_Forces.Dispose();
				m_SB_NodeSims[1].Dispose();
				m_SB_NodeSims[0].Dispose();
				m_SB_Links.Dispose();
				m_SB_Nodes.Dispose();

				m_shader_RenderGraph.Dispose();
				m_shader_Simulate.Dispose();
				m_shader_ComputeForces.Dispose();

				m_CB_Simulation.Dispose();
				m_CB_Main.Dispose();

				Device	temp = m_device;
				m_device = null;
				temp.Dispose();
			}
			base.Dispose( disposing );
		}

		#endregion

		#region EVENT HANDLERS

		private void buttonReset_Click( object sender, EventArgs e ) {
#if true
			for ( int neuronIndex=0; neuronIndex < m_nodesCount; neuronIndex++ ) {
				float	a = Mathf.TWOPI * neuronIndex / m_nodesCount;
				m_SB_NodeSims[0].m[neuronIndex].m_position.Set( 2.0f * (float) SimpleRNG.GetUniform() - 1.0f, 2.0f * (float) SimpleRNG.GetUniform() - 1.0f );	// In a size 2 square
				m_SB_NodeSims[0].m[neuronIndex].m_velocity.Set( 0, 0 );
			}
			m_SB_NodeSims[0].Write();
#else
			for ( int neuronIndex=0; neuronIndex < m_nodesCount; neuronIndex++ ) {
				float	a = Mathf.TWOPI * neuronIndex / m_nodesCount;
				m_SB_NodeSims[0].m[neuronIndex].m_position.Set( Mathf.Cos( a ), Mathf.Sin( a ) );	// Set on a unit circle
				m_SB_NodeSims[0].m[neuronIndex].m_velocity.Set( 0, 0 );
			}
			m_SB_NodeSims[0].Write();
#endif
		}

		private void buttonResetAll_Click( object sender, EventArgs e ) {
		}

		private void buttonResetObstacles_Click( object sender, EventArgs e ) {
		}

		private void buttonReload_Click( object sender, EventArgs e ) {
			m_device.ReloadModifiedShaders();
		}

		private void checkBoxRun_CheckedChanged( object sender, EventArgs e ) {
		}

		private void panelOutput_MouseDown( object sender, MouseEventArgs e ) {
// 			if ( e.Button == MouseButtons.Middle ) {
// 				// Add a new hotspot
// 				AddHotSpot( new Point( e.X * GRAPH_SIZE / panelOutput.Width, e.Y * GRAPH_SIZE / panelOutput.Height ) );
// 
// 				// Authorize source plotting ONLY when we successfully registered a new point
// 				m_plotSource = true;
// 			}
		}

		#endregion
	}
}
