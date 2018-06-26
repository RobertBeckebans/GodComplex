﻿//#define PRECOMPUTE_BRDF	// Define this to precompute the BRDF, comment to only load it ► WARNING ► This code is not functional! Tables are now generated by a mathematica notebook!
//#define TEST_SH_ENVIRONMENT	// Define this to test MSBRDF lighting by an SH-encoded environment
#define TEST_LTC_AREA_LIGHT		// Define this to test MSBRDF lighting by an LTC area light

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SharpMath;
using Renderer;
using System.IO;
using Nuaj.Cirrus.Utility;

namespace TestMSBRDF {

	public partial class TestForm : Form {

		#region CONSTANTS

		const int	NOISE_SIZE = 64;
		const uint	GROUPS_COUNT = 271;

		#endregion

		#region NESTED TYPES

		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		private struct CB_Global {
			public float4		_ScreenSize;
			public float		_Time;
		}

		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		private struct CB_Camera {
			public float4x4		_Camera2World;
			public float4x4		_World2Camera;
			public float4x4		_Proj2World;
			public float4x4		_World2Proj;
			public float4x4		_Camera2Proj;
			public float4x4		_Proj2Camera;
		}

		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		private struct CB_Render {
			public uint			_flags;
			public uint			_groupsCount;
			public uint			_groupIndex;
			public float		_lightElevation;

			public float		_roughnessSphereSpecular;
			public float		_reflectanceSphereSpecular;
			public float		_roughnessSphereDiffuse;
			public float		_reflectanceSphereDiffuse;

			public float		_roughnessGround;
			public float		_reflectanceGround;
			public float		_lightIntensity;
		}

		[System.Runtime.InteropServices.StructLayout( System.Runtime.InteropServices.LayoutKind.Sequential )]
		private struct CB_SH {
			public float4		_SH0;
			public float4		_SH1;
			public float4		_SH2;
			public float4		_SH3;
			public float4		_SH4;
			public float4		_SH5;
			public float4		_SH6;
			public float4		_SH7;
			public float4		_SH8;
		}

		#endregion

		#region FIELDS

		private Device						m_device = new Device();

		Texture2D							m_tex_BlueNoise;
		Texture3D							m_tex_Noise;
		Texture3D							m_tex_Noise4D;

		Texture2D							m_tex_MSBRDF_GGX_E;
		Texture2D							m_tex_MSBRDF_GGX_Eavg;
		Texture2D							m_tex_MSBRDF_OrenNayar_E;
		Texture2D							m_tex_MSBRDF_OrenNayar_Eavg;

		Texture2D							m_tex_CubeMap;

		Texture2D							m_tex_Accumulator;

		Shader								m_shader_Accumulate;
		Shader								m_shader_Finalize;

		uint								m_groupCounter;
		uint[]								m_groupShuffle = new uint[GROUPS_COUNT];

		Camera								m_camera = new Camera();
		CameraManipulator					m_manipulator = new CameraManipulator();

		ConstantBuffer<CB_Global>			m_CB_Global;
		ConstantBuffer<CB_Camera>			m_CB_Camera;
		ConstantBuffer<CB_Render>			m_CB_Render;
		ConstantBuffer<CB_SH>				m_CB_SH;


		//////////////////////////////////////////////////////////////////////////
		// Timing
		public System.Diagnostics.Stopwatch	m_StopWatch = new System.Diagnostics.Stopwatch();
		private double						m_Ticks2Seconds;
		public float						m_StartGameTime = 0;
		public float						m_CurrentGameTime = 0;
		public float						m_StartFPSTime = 0;
		public int							m_SumFrames = 0;
		public float						m_AverageFrameTime = 0.0f;

		#endregion

		public TestForm() {
			InitializeComponent();

// Use this to convert any cross HDR representation into a cube map
//ConvertCrossToCubeMap( new FileInfo( "beach_cross.hdr" ), new FileInfo( "beach.dds" ) );

			Application.Idle += new EventHandler( Application_Idle );
		}

		protected override void OnLoad( EventArgs e ) {
			base.OnLoad( e );

			try {
				m_device.Init( panelOutput.Handle, false, true );
			} catch ( Exception _e ) {
				m_device = null;
				MessageBox.Show( "Failed to initialize DX device!\n\n" + _e.Message, "MSBRDF Test", MessageBoxButtons.OK, MessageBoxIcon.Error );
				return;
			}

			try {
//				m_shader_Render = new Shader( m_device, new System.IO.FileInfo( "Shaders/Render.hlsl" ), VERTEX_FORMAT.Pt4, "VS", null, "PS", null );						// OBSOLETE MSBRDF CODE! For historical purpose only...
 				m_shader_Finalize = new Shader( m_device, new System.IO.FileInfo( "Shaders/RenderComplete.hlsl" ), VERTEX_FORMAT.Pt4, "VS", null, "PS_Finalize", null );
				#if TEST_SH_ENVIRONMENT
					m_shader_Accumulate = new Shader( m_device, new System.IO.FileInfo( "Shaders/RenderCompareSH.hlsl" ), VERTEX_FORMAT.Pt4, "VS", null, "PS", null );			// Use this to show a rendering with SH environment
					checkBoxUseRealTimeApprox.Visible = true;
					checkBoxUseRealTimeApprox.Checked = true;
					floatTrackbarControlRoughnessSphere.Value = 1;		// Show full roughness
					floatTrackbarControlReflectanceSphere2.Value = 0;	// Disturbing if diffuse is showing!
					groupBoxPlane.Visible = false;	// No plane is available in this configuration
				#elif TEST_LTC_AREA_LIGHT
					m_shader_Accumulate = new Shader( m_device, new System.IO.FileInfo( "Shaders/RenderCompareLTC.hlsl" ), VERTEX_FORMAT.Pt4, "VS", null, "PS", null );			// Use this to show a rendering with LTC area light
					checkBoxUseRealTimeApprox.Visible = true;
					checkBoxUseRealTimeApprox.Checked = true;
					floatTrackbarControlRoughnessSphere.Value = 1;		// Show full roughness
					floatTrackbarControlReflectanceSphere2.Value = 0;	// Disturbing if diffuse is showing!
				#else
 					m_shader_Accumulate = new Shader( m_device, new System.IO.FileInfo( "Shaders/RenderComplete.hlsl" ), VERTEX_FORMAT.Pt4, "VS", null, "PS", null );			// Use this for a full render
				#endif
//				m_shader_Accumulate = new Shader( m_device, new System.IO.FileInfo( "Shaders/RenderCompareLCT.hlsl" ), VERTEX_FORMAT.Pt4, "VS", null, "PS", null );			// Use this to show a rendering with LCT area lights (TODO!!)
			} catch ( Exception _e ) {
				MessageBox.Show( "Shader failed to compile!\n\n" + _e.Message, "MSBRDF Test", MessageBoxButtons.OK, MessageBoxIcon.Error );
			}

			uint	W = (uint) panelOutput.Width;
			uint	H = (uint) panelOutput.Height;

			m_CB_Global = new ConstantBuffer<CB_Global>( m_device, 0 );
			m_CB_Camera = new ConstantBuffer<CB_Camera>( m_device, 1 );
			m_CB_Render = new ConstantBuffer<CB_Render>( m_device, 2 );
			m_CB_SH = new ConstantBuffer<CB_SH>( m_device, 3 );

			BuildNoiseTextures();

			// Shuffle group indices
			for ( uint groupIndex=0; groupIndex < GROUPS_COUNT; groupIndex++ )
				m_groupShuffle[groupIndex] = groupIndex;
			for ( uint shuffleIndex=100*GROUPS_COUNT; shuffleIndex > 0; shuffleIndex-- ) {
				for ( uint groupIndex=0; groupIndex < GROUPS_COUNT; groupIndex++ ) {
					uint	i0 = SimpleRNG.GetUint()  % GROUPS_COUNT;
					uint	i1 = SimpleRNG.GetUint()  % GROUPS_COUNT;
					uint	temp = m_groupShuffle[i0];
					m_groupShuffle[i0] = m_groupShuffle[i1];
					m_groupShuffle[i1] = temp;
				}
			}

//BuildMSBRDF( new DirectoryInfo( @".\Tables\" ) );
			LoadMSBRDF( 128, new FileInfo( "./Tables/MSBRDF_GGX_E128x128.float" ), new FileInfo( "./Tables/MSBRDF_GGX_Eavg128.float" ), out m_tex_MSBRDF_GGX_E, out m_tex_MSBRDF_GGX_Eavg );
			LoadMSBRDF( 32, new FileInfo( "./Tables/MSBRDF_OrenNayar_E32x32.float" ), new FileInfo( "./Tables/MSBRDF_OrenNayar_Eavg32.float" ), out m_tex_MSBRDF_OrenNayar_E, out m_tex_MSBRDF_OrenNayar_Eavg );

			#if TEST_LTC_AREA_LIGHT
				// Area light
FitLTC( new DirectoryInfo( @".\Tables\" ) );
			#endif

			// Load cube map
			using ( ImageUtility.ImagesMatrix I = new ImageUtility.ImagesMatrix() ) {
//				I.DDSLoadFile( new FileInfo( "garage4_hd.dds" ) );
				I.DDSLoadFile( new FileInfo( "beach.dds" ) );
				EncodeCubeMapIntoSH( I );
				m_tex_CubeMap = new Texture2D( m_device, I, ImageUtility.COMPONENT_FORMAT.AUTO );
			}

			m_tex_Accumulator = new Texture2D( m_device, m_device.DefaultTarget.Width, m_device.DefaultTarget.Height, 1, 1, ImageUtility.PIXEL_FORMAT.RGBA32F, ImageUtility.COMPONENT_FORMAT.AUTO, false, true, null );

			// Setup camera
			m_camera.CreatePerspectiveCamera( (float) (60.0 * Math.PI / 180.0), (float) panelOutput.Width / panelOutput.Height, 0.01f, 100.0f );
			m_manipulator.Attach( panelOutput, m_camera );
//			m_manipulator.InitializeCamera( new float3( 0, 1.5f, 2.0f ), new float3( -0.4f, 0, 0.4f ), float3.UnitY );					// Garage probe
			m_manipulator.InitializeCamera( new float3( 1.46070266f, 1.10467184f, 1.36212754f ), new float3( 0, 1, 0 ), float3.UnitY );	// Beach probe

			m_camera.CameraTransformChanged += Camera_CameraTransformChanged;
			Camera_CameraTransformChanged( null, EventArgs.Empty );

			// Start game time
			m_Ticks2Seconds = 1.0 / System.Diagnostics.Stopwatch.Frequency;
			m_StopWatch.Start();
			m_StartGameTime = GetGameTime();
		}

		void Application_Idle( object sender, EventArgs e ) {
			if ( m_device == null || checkBoxPause.Checked )
				return;

			uint	W = (uint) panelOutput.Width;
			uint	H = (uint) panelOutput.Height;

			// Timer
			float	lastGameTime = m_CurrentGameTime;
			m_CurrentGameTime = GetGameTime();
			
			if ( m_CurrentGameTime - m_StartFPSTime > 1.0f ) {
				m_AverageFrameTime = (m_CurrentGameTime - m_StartFPSTime) / Math.Max( 1, m_SumFrames );
				m_SumFrames = 0;
				m_StartFPSTime = m_CurrentGameTime;
			}
			m_SumFrames++;

			m_CB_Global.m._ScreenSize.Set( W, H, 1.0f / W, 1.0f / H );
			m_CB_Global.m._Time = m_CurrentGameTime;
			m_CB_Global.UpdateData();

			m_device.ClearDepthStencil( m_device.DefaultDepthStencil, 1.0f, 0, true, false );

			m_tex_Noise.Set( 8 );
			m_tex_Noise4D.Set( 9 );


			//////////////////////////////////////////////////////////////////////////
			// Fullscreen rendering
			if ( (m_groupCounter < GROUPS_COUNT || checkBoxKeepSampling.Checked) && m_shader_Accumulate.Use() ) {
				if ( m_groupCounter == 0 ) {
					m_device.Clear( m_tex_Accumulator, float4.Zero );		// Clear accumulation buffer
				}

				m_device.SetRenderStates( RASTERIZER_STATE.CULL_NONE, DEPTHSTENCIL_STATE.DISABLED, BLEND_STATE.ADDITIVE );
				m_device.SetRenderTarget( m_tex_Accumulator, null );

				m_CB_Render.m._flags = 0;
				m_CB_Render.m._flags |= checkBoxEnableMSBRDF.Checked ? 1U : 0;
				m_CB_Render.m._flags |= checkBoxEnableMSFactor.Checked ? 2U : 0;
				m_CB_Render.m._flags |= checkBoxUseRealTimeApprox.Checked ? 0x100U : 0;
				m_CB_Render.m._groupsCount = GROUPS_COUNT;
				m_CB_Render.m._groupIndex = m_groupShuffle[m_groupCounter % GROUPS_COUNT];
				m_CB_Render.m._lightElevation = floatTrackbarControlLightElevation.Value * Mathf.HALFPI;
				
				m_CB_Render.m._roughnessSphereSpecular = floatTrackbarControlRoughnessSphere.Value;
				m_CB_Render.m._roughnessSphereDiffuse = floatTrackbarControlRoughnessSphere2.Value;
				m_CB_Render.m._roughnessGround = floatTrackbarControlRoughnessGround.Value;

// More linear feel when squaring roughnesses!
m_CB_Render.m._roughnessSphereSpecular *= m_CB_Render.m._roughnessSphereSpecular;
m_CB_Render.m._roughnessSphereDiffuse *= m_CB_Render.m._roughnessSphereDiffuse;
m_CB_Render.m._roughnessGround *= m_CB_Render.m._roughnessGround;

				m_CB_Render.m._reflectanceSphereSpecular = floatTrackbarControlReflectanceSphere.Value;
				m_CB_Render.m._reflectanceSphereDiffuse = floatTrackbarControlReflectanceSphere2.Value;
				m_CB_Render.m._reflectanceGround = floatTrackbarControlReflectanceGround.Value;

				m_CB_Render.m._lightIntensity = floatTrackbarControlCubeMapIntensity.Value;

				m_CB_Render.UpdateData();

				m_tex_CubeMap.SetPS( 0 );
				m_tex_BlueNoise.SetPS( 1 );

				m_tex_MSBRDF_GGX_E.SetPS( 2 );
				m_tex_MSBRDF_GGX_Eavg.SetPS( 3 );
				m_tex_MSBRDF_OrenNayar_E.SetPS( 4 );
				m_tex_MSBRDF_OrenNayar_Eavg.SetPS( 5 );

				m_device.RenderFullscreenQuad( m_shader_Accumulate );

				m_groupCounter++;
			}

			//////////////////////////////////////////////////////////////////////////
			// Finalize accumulation buffer
			if ( m_shader_Finalize.Use() ) {
				m_device.SetRenderStates( RASTERIZER_STATE.CULL_NONE, DEPTHSTENCIL_STATE.DISABLED, BLEND_STATE.DISABLED );
				m_device.SetRenderTarget( m_device.DefaultTarget, null );

				m_tex_Accumulator.SetPS( 6 );

				m_device.RenderFullscreenQuad( m_shader_Finalize );

				m_tex_Accumulator.RemoveFromLastAssignedSlots();
			}

			// Show!
			m_device.Present( false );

			// Update window text
			Text = "MSBRDF Test - Avg. Frame Time " + (1000.0f * m_AverageFrameTime).ToString( "G5" ) + " ms (" + (1.0f / m_AverageFrameTime).ToString( "G5" ) + " FPS)";
		}

		protected override void OnFormClosed( FormClosedEventArgs e ) {
			if ( m_device == null )
				return;

			m_tex_Noise4D.Dispose();
			m_tex_Noise.Dispose();
			m_tex_BlueNoise.Dispose();

			m_tex_MSBRDF_OrenNayar_Eavg.Dispose();
			m_tex_MSBRDF_OrenNayar_E.Dispose();
			m_tex_MSBRDF_GGX_Eavg.Dispose();
			m_tex_MSBRDF_GGX_E.Dispose();

			m_tex_CubeMap.Dispose();

			m_tex_Accumulator.Dispose();

			m_CB_SH.Dispose();
			m_CB_Render.Dispose();
			m_CB_Camera.Dispose();
			m_CB_Global.Dispose();

			m_shader_Accumulate.Dispose();
			m_shader_Finalize.Dispose();

			m_device.Exit();

			base.OnFormClosed( e );
		}

		#region  Noise Generation

		void	BuildNoiseTextures() {

			PixelsBuffer	Content = new PixelsBuffer( NOISE_SIZE*NOISE_SIZE*NOISE_SIZE*4 );
			PixelsBuffer	Content4D = new PixelsBuffer( NOISE_SIZE*NOISE_SIZE*NOISE_SIZE*16 );

			SimpleRNG.SetSeed( 521288629, 362436069 );

			float4	V = float4.Zero;
			using ( BinaryWriter W = Content.OpenStreamWrite() ) {
				using ( BinaryWriter W2 = Content4D.OpenStreamWrite() ) {
					for ( int Z=0; Z < NOISE_SIZE; Z++ )
						for ( int Y=0; Y < NOISE_SIZE; Y++ )
							for ( int X=0; X < NOISE_SIZE; X++ ) {
								V.Set( (float) SimpleRNG.GetUniform(), (float) SimpleRNG.GetUniform(), (float) SimpleRNG.GetUniform(), (float) SimpleRNG.GetUniform() );
								W.Write( V.x );
								W2.Write( V.x );
								W2.Write( V.y );
								W2.Write( V.z );
								W2.Write( V.w );
							}
				}
			}

			m_tex_Noise = new Texture3D( m_device, NOISE_SIZE, NOISE_SIZE, NOISE_SIZE, 1, ImageUtility.PIXEL_FORMAT.R8, ImageUtility.COMPONENT_FORMAT.UNORM, false, false, new PixelsBuffer[] { Content } );
			m_tex_Noise4D = new Texture3D( m_device, NOISE_SIZE, NOISE_SIZE, NOISE_SIZE, 1, ImageUtility.PIXEL_FORMAT.RGBA8, ImageUtility.COMPONENT_FORMAT.UNORM, false, false, new PixelsBuffer[] { Content4D } );

			// Load blue noise
			using ( ImageUtility.ImageFile I = new ImageUtility.ImageFile( new FileInfo( "BlueNoise64x64_16bits.png" ) ) ) {
				ImageUtility.ImagesMatrix M = new ImageUtility.ImagesMatrix( new ImageUtility.ImageFile[,] { { I } }  );
					m_tex_BlueNoise = new Texture2D( m_device, M, ImageUtility.COMPONENT_FORMAT.UNORM );
			}
		}

		#endregion

		#region Multiple-Scattering BRDF

// ACTUALLY, THESE ARE GENERATED IN MATHEMATICA NOW!!

		const uint		COS_THETA_SUBDIVS_COUNT = 128;
		const uint		ROUGHNESS_SUBDIVS_COUNT = 128;

		float[,]	m_GGX_E = new float[COS_THETA_SUBDIVS_COUNT,ROUGHNESS_SUBDIVS_COUNT];
		float[]		m_GGX_Eavg = new float[ROUGHNESS_SUBDIVS_COUNT];
		
		void	BuildMSBRDF( DirectoryInfo _targetDirectory ) {

			FileInfo	MSBRDFFileName = new FileInfo( Path.Combine( _targetDirectory.FullName, "MSBRDF_E" + COS_THETA_SUBDIVS_COUNT + "x" + ROUGHNESS_SUBDIVS_COUNT + ".float" ) );
			FileInfo	MSBRDFFileName2 = new FileInfo( Path.Combine( _targetDirectory.FullName, "MSBRDF_Eavg" + ROUGHNESS_SUBDIVS_COUNT + ".float" ) );

			#if PRECOMPUTE_BRDF

ACTUALLY, THESE ARE GENERATED IN MATHEMATICA NOW!!
Don't expect this code to work!

{
			const uint		PHI_SUBDIVS_COUNT = 2*512;
			const uint		THETA_SUBDIVS_COUNT = 64;

			const float		dPhi = Mathf.TWOPI / PHI_SUBDIVS_COUNT;
			const float		dTheta = Mathf.HALFPI / THETA_SUBDIVS_COUNT;
			const float		dMu = 1.0f / THETA_SUBDIVS_COUNT;

			string	dumpMathematica = "{";
			for ( uint Y=0; Y < ROUGHNESS_SUBDIVS_COUNT; Y++ ) {

//Y = 5;

				float	m = (float) Y / (ROUGHNESS_SUBDIVS_COUNT-1);
				float	m2 = Math.Max( 0.01f, m*m );
// 				float	m2 = Math.Max( 0.01f, (float) Y / (ROUGHNESS_SUBDIVS_COUNT-1) );
// 				float	m = Mathf.Sqrt( m2 );

//				dumpMathematica += "{ ";	// Start a new roughness line
				for ( uint X=0; X < COS_THETA_SUBDIVS_COUNT; X++ ) {

//X = 17;

					float	cosThetaO = (float) X / (COS_THETA_SUBDIVS_COUNT-1);
					float	sinThetaO = Mathf.Sqrt( 1 - cosThetaO*cosThetaO );

					float	NdotV = cosThetaO;

					float	integral = 0.0f;
//					float	integralNDF = 0.0f;
					for ( uint THETA=0; THETA < THETA_SUBDIVS_COUNT; THETA++ ) {
// 						float	thetaI = Mathf.HALFPI * (0.5f+THETA) / THETA_SUBDIVS_COUNT;
// 						float	cosThetaI = Mathf.Cos( thetaI );
// 						float	sinThetaI = Mathf.Sin( thetaI );

						// Use cosine-weighted sampling
						float	sqCosThetaI = (0.5f+THETA) / THETA_SUBDIVS_COUNT;
						float	cosThetaI = Mathf.Sqrt( sqCosThetaI );
						float	sinThetaI = Mathf.Sqrt( 1 - sqCosThetaI );

						float	NdotL = cosThetaI;

						for ( uint PHI=0; PHI < PHI_SUBDIVS_COUNT; PHI++ ) {
							float	phi = Mathf.TWOPI * PHI / PHI_SUBDIVS_COUNT;

							// Compute cos(theta_h) = Omega_h.N where Omega_h = (Omega_i + Omega_o) / ||Omega_i + Omega_o|| is the half vector and N the surface normal
							float	cosThetaH = (cosThetaI + cosThetaO) / Mathf.Sqrt( 2 * (1 + cosThetaO * cosThetaI + sinThetaO * sinThetaI * Mathf.Cos( phi )) );
// 							float3	omega_i = new float3( sinThetaI * Mathf.Cos( phi ), sinThetaI * Mathf.Sin( phi ), cosThetaI );
// 							float3	omega_o = new float3( sinThetaO, 0, cosThetaO );
// 							float3	omega_h = (omega_i + omega_o).Normalized;
// 							float	cosThetaH = omega_h.z;

							// Compute GGX NDF
							float	den = 1 - cosThetaH*cosThetaH * (1 - m2);
							float	NDF = m2 / (Mathf.PI * den*den);

							// Compute Smith shadowing/masking
							float	Smith_i_den = NdotL + Mathf.Sqrt( m2 + (1-m2) * NdotL*NdotL );

							// Full BRDF is thus...
							float	GGX = NDF / Smith_i_den;

//							integral += GGX * cosThetaI * sinThetaI;
							integral += GGX;
						}

//						integralNDF += Mathf.TWOPI * m2 * cosThetaI * sinThetaI / (Mathf.PI * Mathf.Pow( cosThetaI*cosThetaI * (m2 - 1) + 1, 2.0f ));
					}

					// Finalize
					float	Smith_o_den = NdotV + Mathf.Sqrt( m2 + (1-m2) * NdotV*NdotV );
					integral /= Smith_o_den;

//					integral *= dTheta * dPhi;
					integral *= 0.5f * dMu * dPhi;	// Cosine-weighted sampling has a 0.5 factor!
//					integralNDF *= dTheta;

					m_GGX_E[X,Y] = integral;
					dumpMathematica += "{ " + cosThetaO + ", " + m + ", "  + integral + "}, ";
				}
			}

			dumpMathematica = dumpMathematica.Remove( dumpMathematica.Length-2 );	// Remove last comma
			dumpMathematica += " };";

			// Dump as binary
			using ( FileStream S = MSBRDFFileName.Create() )
				using ( BinaryWriter W = new BinaryWriter( S ) ) {
					for ( uint Y=0; Y < ROUGHNESS_SUBDIVS_COUNT; Y++ )
						for ( uint X=0; X < COS_THETA_SUBDIVS_COUNT; X++ )
							W.Write( m_GGX_E[X,Y] );
				}

			//////////////////////////////////////////////////////////////////////////
			// Compute average irradiance based on roughness, re-using the previously computed results
			const uint		THETA_SUBDIVS_COUNT2 = 512;

			float	dTheta2 = Mathf.HALFPI / THETA_SUBDIVS_COUNT2;

			for ( uint X=0; X < ROUGHNESS_SUBDIVS_COUNT; X++ ) {

				float	integral = 0.0f;
				for ( uint THETA=0; THETA < THETA_SUBDIVS_COUNT2; THETA++ ) {
					float	thetaO = Mathf.HALFPI * (0.5f+THETA) / THETA_SUBDIVS_COUNT2;
					float	cosThetaO = Mathf.Cos( thetaO );
					float	sinThetaO = Mathf.Sin( thetaO );

					// Sample previously computed table
					float	i = cosThetaO * COS_THETA_SUBDIVS_COUNT;
					uint	i0 = Math.Min( COS_THETA_SUBDIVS_COUNT-1, (uint) Mathf.Floor( i ) );
					uint	i1 = Math.Min( COS_THETA_SUBDIVS_COUNT-1, i0 + 1 );
					float	E = (1-i) * m_GGX_E[i0,X] + i * m_GGX_E[i1,X];

					integral += E * cosThetaO * sinThetaO;
				}

				// Finalize
				integral *= Mathf.TWOPI * dTheta2;

				m_GGX_Eavg[X] = integral;
			}

			// Dump as binary
			using ( FileStream S = MSBRDFFileName2.Create() )
				using ( BinaryWriter W = new BinaryWriter( S ) ) {
					for ( uint X=0; X < ROUGHNESS_SUBDIVS_COUNT; X++ )
						W.Write( m_GGX_Eavg[X] );
				}
}

			#endif

// 			// Build irradiance complement texture
// 			using ( PixelsBuffer content = new PixelsBuffer( COS_THETA_SUBDIVS_COUNT * ROUGHNESS_SUBDIVS_COUNT * 4 ) ) {
// 				using ( FileStream S = MSBRDFFileName.OpenRead() )
// 					using ( BinaryReader R = new BinaryReader( S ) )
// 						using ( BinaryWriter W = content.OpenStreamWrite() ) {
// 							for ( uint Y=0; Y < ROUGHNESS_SUBDIVS_COUNT; Y++ ) {
// 								for ( uint X=0; X < COS_THETA_SUBDIVS_COUNT; X++ ) {
// 									float	V = R.ReadSingle();
// 									m_GGX_E[X,Y] = V;
// 									W.Write( V );
// 								}
// 							}
// 						}
// 
// 				m_tex_IrradianceComplement = new Texture2D( m_device, COS_THETA_SUBDIVS_COUNT, ROUGHNESS_SUBDIVS_COUNT, 1, 1, ImageUtility.PIXEL_FORMAT.R32F, ImageUtility.COMPONENT_FORMAT.AUTO, false, false, new PixelsBuffer[] { content } );
// 			}
// 
// 			// Build average irradiance texture
// 			using ( PixelsBuffer content = new PixelsBuffer( ROUGHNESS_SUBDIVS_COUNT * 4 ) ) {
// 				using ( FileStream S = MSBRDFFileName2.OpenRead() )
// 					using ( BinaryReader R = new BinaryReader( S ) )
// 						using ( BinaryWriter W = content.OpenStreamWrite() ) {
// 							for ( uint X=0; X < ROUGHNESS_SUBDIVS_COUNT; X++ ) {
// 								float	V = R.ReadSingle();
// 								m_GGX_Eavg[X] = V;
// 								W.Write( V );
// 							}
// 						}
// 
// 				m_tex_IrradianceAverage = new Texture2D( m_device, ROUGHNESS_SUBDIVS_COUNT, 1, 1, 1, ImageUtility.PIXEL_FORMAT.R32F, ImageUtility.COMPONENT_FORMAT.AUTO, false, false, new PixelsBuffer[] { content } );
// 			}


//////////////////////////////////////////////////////////////////////////
// Check single-scattering and multiple-scattering BRDFs are actual complements
//
/*
float3[,]	integralChecks = new float3[COS_THETA_SUBDIVS_COUNT,ROUGHNESS_SUBDIVS_COUNT];
for ( uint Y=0; Y < ROUGHNESS_SUBDIVS_COUNT; Y++ ) {
	float	m = (float) Y / (ROUGHNESS_SUBDIVS_COUNT-1);
	float	m2 = Math.Max( 0.01f, m*m );

	float	Eavg = SampleEavg( m );

	for ( uint X=0; X < COS_THETA_SUBDIVS_COUNT; X++ ) {
		float	cosThetaO = (float) X / (COS_THETA_SUBDIVS_COUNT-1);
		float	sinThetaO = Mathf.Sqrt( 1 - cosThetaO*cosThetaO );

		float	NdotV = cosThetaO;

		float	Eo = SampleE( cosThetaO, m );

		const uint		CHECK_THETA_SUBDIVS_COUNT = 128;
		const uint		CHECK_PHI_SUBDIVS_COUNT = 2*128;

		const float		dPhi = Mathf.TWOPI / CHECK_PHI_SUBDIVS_COUNT;
		const float		dTheta = Mathf.HALFPI / CHECK_THETA_SUBDIVS_COUNT;

		float	integralSS = 0.0f;
		float	integralMS = 0.0f;
		for ( uint THETA=0; THETA < CHECK_THETA_SUBDIVS_COUNT; THETA++ ) {

			// Use regular sampling
			float	thetaI = Mathf.HALFPI * (0.5f+THETA) / CHECK_THETA_SUBDIVS_COUNT;
			float	cosThetaI = Mathf.Cos( thetaI );
			float	sinThetaI = Mathf.Sin( thetaI );

// 			// Use cosine-weighted sampling
// 			float	sqCosThetaI = (0.5f+THETA) / CHECK_THETA_SUBDIVS_COUNT;
// 			float	cosThetaI = Mathf.Sqrt( sqCosThetaI );
// 			float	sinThetaI = Mathf.Sqrt( 1 - sqCosThetaI );

 			float	NdotL = cosThetaI;

			for ( uint PHI=0; PHI < CHECK_PHI_SUBDIVS_COUNT; PHI++ ) {
				float	phi = Mathf.TWOPI * PHI / CHECK_PHI_SUBDIVS_COUNT;

				//////////////////////////////////////////////////////////////////////////
				// Single-scattering part

				// Compute cos(theta_h) = Omega_h.N where Omega_h = (Omega_i + Omega_o) / ||Omega_i + Omega_o|| is the half vector and N the surface normal
				float	cosThetaH = (cosThetaI + cosThetaO) / Mathf.Sqrt( 2 * (1 + cosThetaO * cosThetaI + sinThetaO * sinThetaI * Mathf.Sin( phi )) );
// 				float3	omega_i = new float3( sinThetaI * Mathf.Cos( phi ), sinThetaI * Mathf.Sin( phi ), cosThetaI );
// 				float3	omega_o = new float3( sinThetaO, 0, cosThetaO );
// 				float3	omega_h = (omega_i + omega_o).Normalized;
// 				float	cosThetaH = omega_h.z;

				// Compute GGX NDF
				float	den = 1 - cosThetaH*cosThetaH * (1 - m2);
				float	NDF = m2 / (Mathf.PI * den*den);

				// Compute Smith shadowing/masking
				float	Smith_i_den = NdotL + Mathf.Sqrt( m2 + (1-m2) * NdotL*NdotL );
				float	Smith_o_den = NdotV + Mathf.Sqrt( m2 + (1-m2) * NdotV*NdotV );

				// Full BRDF is thus...
				float	GGX = NDF / (Smith_i_den * Smith_o_den);

				integralSS += GGX * cosThetaI * sinThetaI;
//				integralSS += GGX;

				//////////////////////////////////////////////////////////////////////////
				// Multiple-scattering part
				float	Ei = SampleE( cosThetaI, m );

				float	GGX_ms = Eo * Ei / Eavg;

				integralMS += GGX_ms * cosThetaI * sinThetaI;
			}
		}

		// Finalize
		integralSS *= dTheta * dPhi;
		integralMS *= dTheta * dPhi;

		integralChecks[X,Y] = new float3( integralSS, integralMS, integralSS + integralMS );
	}
}
//*/
//////////////////////////////////////////////////////////////////////////


// verify BRDF + BRDFms integration = 1
//cube map + integration
		}

		float	SampleE( float _cosTheta, float _roughness ) {
			_cosTheta *= COS_THETA_SUBDIVS_COUNT;
			_roughness *= ROUGHNESS_SUBDIVS_COUNT;

			float	X = Mathf.Floor( _cosTheta );
			float	x = _cosTheta - X;
			uint	X0 = Mathf.Min( COS_THETA_SUBDIVS_COUNT-1, (uint) X );
			uint	X1 = Mathf.Min( COS_THETA_SUBDIVS_COUNT-1, X0+1 );

			float	Y = Mathf.Floor( _roughness );
			float	y = _roughness - Y;
			uint	Y0 = Mathf.Min( ROUGHNESS_SUBDIVS_COUNT-1, (uint) Y );
			uint	Y1 = Mathf.Min( ROUGHNESS_SUBDIVS_COUNT-1, Y0+1 );

			float	V00 = m_GGX_E[X0,Y0];
			float	V10 = m_GGX_E[X1,Y0];
			float	V01 = m_GGX_E[X0,Y1];
			float	V11 = m_GGX_E[X1,Y1];

			float	V0 = (1.0f - x) * V00 + x * V10;
			float	V1 = (1.0f - x) * V01 + x * V11;
			float	V = (1.0f - y) * V0 + y * V1;
			return V;
		}
		float	SampleEavg( float _roughness ) {
			_roughness *= ROUGHNESS_SUBDIVS_COUNT;
			float	X = Mathf.Floor( _roughness );
			float	x = _roughness - X;
			uint	X0 = Mathf.Min( ROUGHNESS_SUBDIVS_COUNT-1, (uint) X );
			uint	X1 = Mathf.Min( ROUGHNESS_SUBDIVS_COUNT-1, X0+1 );

			float	V0 = m_GGX_Eavg[X0];
			float	V1 = m_GGX_Eavg[X1];
			float	V = (1.0f - x) * V0 + x * V1;
			return V;
		}

		void	LoadMSBRDF( int _size, FileInfo _irradianceTableName, FileInfo _whiteFurnaceTableName, out Texture2D _irradianceTexture, out Texture2D _whiteFurnaceTexture ) {

			// Read irradiance tables
			float[,]		irradianceTable = new float[_size,_size];
			PixelsBuffer	contentIrradiance = new PixelsBuffer( (uint) (_size*_size*4) );
			using ( FileStream S = _irradianceTableName.OpenRead() )
				using ( BinaryReader R = new BinaryReader( S ) ) {
					using ( BinaryWriter W = contentIrradiance.OpenStreamWrite() ) {
						for ( int Y=0; Y < _size; Y++ ) {
							for ( int X=0; X < _size; X++ ) {
								float	V = R.ReadSingle();
								irradianceTable[X,Y] = V;
								W.Write( V );
							}
						}
					}
				}

			// Read white furnace table
			float[]			whiteFurnaceTable = new float[_size];
			PixelsBuffer	contentWhiteFurnace = new PixelsBuffer( (uint) (_size*4) );
			using ( FileStream S = _whiteFurnaceTableName.OpenRead() )
				using ( BinaryReader R = new BinaryReader( S ) ) {
					using ( BinaryWriter W = contentWhiteFurnace.OpenStreamWrite() ) {
						for ( int Y=0; Y < _size; Y++ ) {
							float	V = R.ReadSingle();
							whiteFurnaceTable[Y] = V;
							W.Write( V );
						}
					}
				}

			// Create textures
			_irradianceTexture = new Texture2D( m_device, (uint) _size, (uint) _size, 1, 1, ImageUtility.PIXEL_FORMAT.R32F, ImageUtility.COMPONENT_FORMAT.AUTO, false, true, new PixelsBuffer[] { contentIrradiance } );
			_whiteFurnaceTexture = new Texture2D( m_device, (uint) _size, 1, 1, 1, ImageUtility.PIXEL_FORMAT.R32F, ImageUtility.COMPONENT_FORMAT.AUTO, false, true, new PixelsBuffer[] { contentWhiteFurnace } );
		}

		#endregion

		#region LTC Area Lights

		void	FitLTC( DirectoryInfo _targetDirectory ) {
			LTC.LTCFitter	fitter = new LTC.LTCFitter( this, true );

			LTC.BRDF_GGX	GGX = new LTC.BRDF_GGX();
			fitter.Fit( GGX, 64, new FileInfo( "GGX.ltc" ) );

// 			LTC.BRDF_GGX_NoView	GGX = new LTC.BRDF_GGX_NoView();
// 			fitter.Fit( GGX, 64, new FileInfo( "GGX_NoView.ltc" ) );
		}

		#endregion

		#region Cross -> Cube Map Conversion

		void	ConvertCrossToCubeMap( FileInfo _crossMapFile, FileInfo _cubeMapFile ) {
			using ( ImageUtility.ImageFile crossMap = new ImageUtility.ImageFile( _crossMapFile ) ) {
				using ( ImageUtility.ImagesMatrix	cubeMap = new ImageUtility.ImagesMatrix() ) {
					cubeMap.ConvertCrossToCubeMap( crossMap, true );
					cubeMap.DDSSaveFile( _cubeMapFile, ImageUtility.COMPONENT_FORMAT.AUTO );

// Optional: BC6H Compress
//ImageUtility.ImagesMatrix	compressedCubeMap = m_device.DDSCompress( cubeMap, ImageUtility.ImagesMatrix.COMPRESSION_TYPE.BC6H, ImageUtility.COMPONENT_FORMAT.AUTO );
//compressedCubeMap.DDSSaveFile( _cubeMapFile, ImageUtility.COMPONENT_FORMAT.AUTO );
				}
			}
		}

		#endregion

		#region 

		void		EncodeCubeMapIntoSH( ImageUtility.ImagesMatrix _cubemap ) {
			float3[]	envSH = _cubemap.EncodeSHOrder2();

			SphericalHarmonics.SHFunctions.FilterHanning( envSH, 2.8f );	// Important otherwise banding occurs!

			m_CB_SH.m._SH0.Set( envSH[0], 0 );
			m_CB_SH.m._SH1.Set( envSH[1], 0 );
			m_CB_SH.m._SH2.Set( envSH[2], 0 );
			m_CB_SH.m._SH3.Set( envSH[3], 0 );
			m_CB_SH.m._SH4.Set( envSH[4], 0 );
			m_CB_SH.m._SH5.Set( envSH[5], 0 );
			m_CB_SH.m._SH6.Set( envSH[6], 0 );
			m_CB_SH.m._SH7.Set( envSH[7], 0 );
			m_CB_SH.m._SH8.Set( envSH[8], 0 );
			m_CB_SH.UpdateData();


// // Encode as order-9 (100 coefficients), stored into a texture... FOR DEBUG PURPOSE ONLY!!
// float3[]	envSH2 = _cubemap.EncodeSH( 9 );
// 
// PixelsBuffer	content = new PixelsBuffer( 100 * 16 );
// using ( BinaryWriter W = content.OpenStreamWrite() )
// 	for ( uint i=0; i < 100; i++ ) {
// 		W.Write( envSH2[i].x );
// 		W.Write( envSH2[i].y );
// 		W.Write( envSH2[i].z );
// 		W.Write( 0.0f );
// 	}
// 
// Texture2D	texSH = new Texture2D( m_device, 100, 1, 1, 1, ImageUtility.PIXEL_FORMAT.RGBA32F, ImageUtility.COMPONENT_FORMAT.AUTO, false, false, new PixelsBuffer[] { content } );
// 				texSH.Set( 30 );
		}

		#endregion

		void Camera_CameraTransformChanged( object sender, EventArgs e ) {

			m_CB_Camera.m._Camera2World = m_camera.Camera2World;
			m_CB_Camera.m._World2Camera = m_camera.World2Camera;

			m_CB_Camera.m._Camera2Proj = m_camera.Camera2Proj;
			m_CB_Camera.m._Proj2Camera = m_CB_Camera.m._Camera2Proj.Inverse;

			m_CB_Camera.m._World2Proj = m_CB_Camera.m._World2Camera * m_CB_Camera.m._Camera2Proj;
			m_CB_Camera.m._Proj2World = m_CB_Camera.m._Proj2Camera * m_CB_Camera.m._Camera2World;

			m_CB_Camera.UpdateData();

			m_groupCounter = 0;	// Clear buffer
		}

		/// <summary>
		/// Gets the current game time in seconds
		/// </summary>
		/// <returns></returns>
		public float	GetGameTime() {
			long	ticks = m_StopWatch.ElapsedTicks;
			float	time = (float) (ticks * m_Ticks2Seconds);
			return time;
		}

		private void buttonReload_Click( object sender, EventArgs e ) {
			m_device.ReloadModifiedShaders();

			m_groupCounter = 0;	// Clear buffer
		}

		private void floatTrackbarControlRoughnessSpec_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue ) {
			m_groupCounter = 0;	// Clear buffer
		}

		private void floatTrackbarControlRoughnessDiffuse_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue ) {
			m_groupCounter = 0;	// Clear buffer
		}

		private void floatTrackbarControlAlbedo_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue ) {
			m_groupCounter = 0;	// Clear buffer
		}

		private void floatTrackbarControlLightElevation_ValueChanged( FloatTrackbarControl _Sender, float _fFormerValue ) {
			m_groupCounter = 0;	// Clear buffer
		}

		private void checkBoxEnableMSBRDF_CheckedChanged( object sender, EventArgs e ) {
			m_groupCounter = 0;	// Clear buffer
		}

		private void TestForm_KeyDown( object sender, KeyEventArgs e ) {
			if ( e.KeyCode == Keys.Space )
				checkBoxEnableMSBRDF.Checked ^= true;
		}
	}
}
