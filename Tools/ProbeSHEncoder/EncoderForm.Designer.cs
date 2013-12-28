﻿using ProbeSHEncoder;

namespace ProbeSHEncoder
{
	partial class EncoderForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && (components != null) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			WMath.Vector vector4 = new WMath.Vector();
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.convertShaderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openFileDialogShader = new System.Windows.Forms.OpenFileDialog();
			this.saveFileDialogShader = new System.Windows.Forms.SaveFileDialog();
			this.buttonCompute = new System.Windows.Forms.Button();
			this.radioButtonAlbedo = new System.Windows.Forms.RadioButton();
			this.radioButtonDistance = new System.Windows.Forms.RadioButton();
			this.radioButtonSetIndex = new System.Windows.Forms.RadioButton();
			this.radioButtonSetColor = new System.Windows.Forms.RadioButton();
			this.radioButtonNormal = new System.Windows.Forms.RadioButton();
			this.radioButtonSetDistance = new System.Windows.Forms.RadioButton();
			this.radioButtonSetNormal = new System.Windows.Forms.RadioButton();
			this.textBoxResults = new System.Windows.Forms.TextBox();
			this.floatTrackbarControlAlbedo = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label1 = new System.Windows.Forms.Label();
			this.floatTrackbarControlNormal = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label2 = new System.Windows.Forms.Label();
			this.outputPanel1 = new ProbeSHEncoder.OutputPanel( this.components );
			this.floatTrackbarControlPosition = new Nuaj.Cirrus.Utility.FloatTrackbarControl();
			this.label3 = new System.Windows.Forms.Label();
			this.menuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem} );
			this.menuStrip.Location = new System.Drawing.Point( 0, 0 );
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size( 1027, 24 );
			this.menuStrip.TabIndex = 1;
			this.menuStrip.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size( 37, 20 );
			this.fileToolStripMenuItem.Text = "File";
			// 
			// toolsToolStripMenuItem
			// 
			this.toolsToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.convertShaderToolStripMenuItem} );
			this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			this.toolsToolStripMenuItem.Size = new System.Drawing.Size( 48, 20 );
			this.toolsToolStripMenuItem.Text = "Tools";
			// 
			// convertShaderToolStripMenuItem
			// 
			this.convertShaderToolStripMenuItem.Name = "convertShaderToolStripMenuItem";
			this.convertShaderToolStripMenuItem.Size = new System.Drawing.Size( 164, 22 );
			this.convertShaderToolStripMenuItem.Text = "Convert Shader...";
			// 
			// openFileDialogShader
			// 
			this.openFileDialogShader.DefaultExt = "hlsl";
			this.openFileDialogShader.Filter = "HLSL Shader File (*.hlsl)|*.hlsl|All Files (*.*)|*.*";
			this.openFileDialogShader.Title = "Choose a shader file to convert...";
			// 
			// saveFileDialogShader
			// 
			this.saveFileDialogShader.DefaultExt = "cs";
			this.saveFileDialogShader.Filter = "C# Source File (*.cs)|*.cs|All Files (*.*)|*.*";
			this.saveFileDialogShader.Title = "Choose a target C# file to save the converted shader to...";
			// 
			// buttonCompute
			// 
			this.buttonCompute.Location = new System.Drawing.Point( 712, 57 );
			this.buttonCompute.Name = "buttonCompute";
			this.buttonCompute.Size = new System.Drawing.Size( 97, 66 );
			this.buttonCompute.TabIndex = 2;
			this.buttonCompute.Text = "Compute k-Means";
			this.buttonCompute.UseVisualStyleBackColor = true;
			this.buttonCompute.Click += new System.EventHandler( this.buttonCompute_Click );
			// 
			// radioButtonAlbedo
			// 
			this.radioButtonAlbedo.AutoSize = true;
			this.radioButtonAlbedo.Checked = true;
			this.radioButtonAlbedo.Location = new System.Drawing.Point( 722, 174 );
			this.radioButtonAlbedo.Name = "radioButtonAlbedo";
			this.radioButtonAlbedo.Size = new System.Drawing.Size( 58, 17 );
			this.radioButtonAlbedo.TabIndex = 4;
			this.radioButtonAlbedo.TabStop = true;
			this.radioButtonAlbedo.Text = "Albedo";
			this.radioButtonAlbedo.UseVisualStyleBackColor = true;
			this.radioButtonAlbedo.CheckedChanged += new System.EventHandler( this.radioButtonAlbedo_CheckedChanged );
			// 
			// radioButtonDistance
			// 
			this.radioButtonDistance.AutoSize = true;
			this.radioButtonDistance.Location = new System.Drawing.Point( 722, 197 );
			this.radioButtonDistance.Name = "radioButtonDistance";
			this.radioButtonDistance.Size = new System.Drawing.Size( 67, 17 );
			this.radioButtonDistance.TabIndex = 4;
			this.radioButtonDistance.Text = "Distance";
			this.radioButtonDistance.UseVisualStyleBackColor = true;
			this.radioButtonDistance.CheckedChanged += new System.EventHandler( this.radioButtonDistance_CheckedChanged );
			// 
			// radioButtonSetIndex
			// 
			this.radioButtonSetIndex.AutoSize = true;
			this.radioButtonSetIndex.Location = new System.Drawing.Point( 722, 243 );
			this.radioButtonSetIndex.Name = "radioButtonSetIndex";
			this.radioButtonSetIndex.Size = new System.Drawing.Size( 70, 17 );
			this.radioButtonSetIndex.TabIndex = 4;
			this.radioButtonSetIndex.Text = "Set Index";
			this.radioButtonSetIndex.UseVisualStyleBackColor = true;
			this.radioButtonSetIndex.CheckedChanged += new System.EventHandler( this.radioButtonSetIndex_CheckedChanged );
			// 
			// radioButtonSetColor
			// 
			this.radioButtonSetColor.AutoSize = true;
			this.radioButtonSetColor.Location = new System.Drawing.Point( 722, 266 );
			this.radioButtonSetColor.Name = "radioButtonSetColor";
			this.radioButtonSetColor.Size = new System.Drawing.Size( 77, 17 );
			this.radioButtonSetColor.TabIndex = 4;
			this.radioButtonSetColor.Text = "Set Albedo";
			this.radioButtonSetColor.UseVisualStyleBackColor = true;
			this.radioButtonSetColor.CheckedChanged += new System.EventHandler( this.radioButtonSetColor_CheckedChanged );
			// 
			// radioButtonNormal
			// 
			this.radioButtonNormal.AutoSize = true;
			this.radioButtonNormal.Location = new System.Drawing.Point( 722, 220 );
			this.radioButtonNormal.Name = "radioButtonNormal";
			this.radioButtonNormal.Size = new System.Drawing.Size( 58, 17 );
			this.radioButtonNormal.TabIndex = 4;
			this.radioButtonNormal.Text = "Normal";
			this.radioButtonNormal.UseVisualStyleBackColor = true;
			this.radioButtonNormal.CheckedChanged += new System.EventHandler( this.radioButtonNormal_CheckedChanged );
			// 
			// radioButtonSetDistance
			// 
			this.radioButtonSetDistance.AutoSize = true;
			this.radioButtonSetDistance.Location = new System.Drawing.Point( 722, 289 );
			this.radioButtonSetDistance.Name = "radioButtonSetDistance";
			this.radioButtonSetDistance.Size = new System.Drawing.Size( 86, 17 );
			this.radioButtonSetDistance.TabIndex = 4;
			this.radioButtonSetDistance.Text = "Set Distance";
			this.radioButtonSetDistance.UseVisualStyleBackColor = true;
			this.radioButtonSetDistance.CheckedChanged += new System.EventHandler( this.radioButtonSetDistance_CheckedChanged );
			// 
			// radioButtonSetNormal
			// 
			this.radioButtonSetNormal.AutoSize = true;
			this.radioButtonSetNormal.Location = new System.Drawing.Point( 722, 312 );
			this.radioButtonSetNormal.Name = "radioButtonSetNormal";
			this.radioButtonSetNormal.Size = new System.Drawing.Size( 77, 17 );
			this.radioButtonSetNormal.TabIndex = 4;
			this.radioButtonSetNormal.Text = "Set Normal";
			this.radioButtonSetNormal.UseVisualStyleBackColor = true;
			this.radioButtonSetNormal.CheckedChanged += new System.EventHandler( this.radioButtonSetNormal_CheckedChanged );
			// 
			// textBoxResults
			// 
			this.textBoxResults.Location = new System.Drawing.Point( 827, 57 );
			this.textBoxResults.Multiline = true;
			this.textBoxResults.Name = "textBoxResults";
			this.textBoxResults.ReadOnly = true;
			this.textBoxResults.Size = new System.Drawing.Size( 188, 387 );
			this.textBoxResults.TabIndex = 5;
			// 
			// floatTrackbarControlAlbedo
			// 
			this.floatTrackbarControlAlbedo.Location = new System.Drawing.Point( 712, 524 );
			this.floatTrackbarControlAlbedo.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlAlbedo.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlAlbedo.Name = "floatTrackbarControlAlbedo";
			this.floatTrackbarControlAlbedo.RangeMax = 100F;
			this.floatTrackbarControlAlbedo.RangeMin = 0F;
			this.floatTrackbarControlAlbedo.Size = new System.Drawing.Size( 303, 20 );
			this.floatTrackbarControlAlbedo.TabIndex = 6;
			this.floatTrackbarControlAlbedo.Value = 1F;
			this.floatTrackbarControlAlbedo.VisibleRangeMax = 1F;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 709, 508 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 150, 13 );
			this.label1.TabIndex = 7;
			this.label1.Text = "Albedo Separation Importance";
			// 
			// floatTrackbarControlNormal
			// 
			this.floatTrackbarControlNormal.Location = new System.Drawing.Point( 712, 569 );
			this.floatTrackbarControlNormal.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlNormal.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlNormal.Name = "floatTrackbarControlNormal";
			this.floatTrackbarControlNormal.RangeMax = 100F;
			this.floatTrackbarControlNormal.RangeMin = 0F;
			this.floatTrackbarControlNormal.Size = new System.Drawing.Size( 303, 20 );
			this.floatTrackbarControlNormal.TabIndex = 6;
			this.floatTrackbarControlNormal.Value = 1F;
			this.floatTrackbarControlNormal.VisibleRangeMax = 1F;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 709, 553 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 150, 13 );
			this.label2.TabIndex = 7;
			this.label2.Text = "Normal Separation Importance";
			// 
			// outputPanel1
			// 
			vector4.X = 0F;
			vector4.Y = 0F;
			vector4.Z = 1F;
			this.outputPanel1.At = vector4;
			this.outputPanel1.Location = new System.Drawing.Point( 12, 27 );
			this.outputPanel1.Name = "outputPanel1";
			this.outputPanel1.Size = new System.Drawing.Size( 677, 546 );
			this.outputPanel1.TabIndex = 3;
			this.outputPanel1.Viz = ProbeSHEncoder.OutputPanel.VIZ_TYPE.ALBEDO;
			// 
			// floatTrackbarControlPosition
			// 
			this.floatTrackbarControlPosition.Location = new System.Drawing.Point( 712, 476 );
			this.floatTrackbarControlPosition.MaximumSize = new System.Drawing.Size( 10000, 20 );
			this.floatTrackbarControlPosition.MinimumSize = new System.Drawing.Size( 70, 20 );
			this.floatTrackbarControlPosition.Name = "floatTrackbarControlPosition";
			this.floatTrackbarControlPosition.RangeMax = 100F;
			this.floatTrackbarControlPosition.RangeMin = 0F;
			this.floatTrackbarControlPosition.Size = new System.Drawing.Size( 303, 20 );
			this.floatTrackbarControlPosition.TabIndex = 6;
			this.floatTrackbarControlPosition.Value = 1F;
			this.floatTrackbarControlPosition.VisibleRangeMax = 1F;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 709, 460 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 149, 13 );
			this.label3.TabIndex = 7;
			this.label3.Text = "Spatial Separation Importance";
			// 
			// EncoderForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 1027, 610 );
			this.Controls.Add( this.label2 );
			this.Controls.Add( this.label3 );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.floatTrackbarControlPosition );
			this.Controls.Add( this.floatTrackbarControlNormal );
			this.Controls.Add( this.floatTrackbarControlAlbedo );
			this.Controls.Add( this.textBoxResults );
			this.Controls.Add( this.radioButtonSetNormal );
			this.Controls.Add( this.radioButtonSetDistance );
			this.Controls.Add( this.radioButtonSetColor );
			this.Controls.Add( this.radioButtonSetIndex );
			this.Controls.Add( this.radioButtonNormal );
			this.Controls.Add( this.radioButtonDistance );
			this.Controls.Add( this.radioButtonAlbedo );
			this.Controls.Add( this.outputPanel1 );
			this.Controls.Add( this.buttonCompute );
			this.Controls.Add( this.menuStrip );
			this.MainMenuStrip = this.menuStrip;
			this.Name = "EncoderForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Probe SH Encoder";
			this.menuStrip.ResumeLayout( false );
			this.menuStrip.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem convertShaderToolStripMenuItem;
		private System.Windows.Forms.OpenFileDialog openFileDialogShader;
		private System.Windows.Forms.SaveFileDialog saveFileDialogShader;
		private System.Windows.Forms.Button buttonCompute;
		private OutputPanel outputPanel1;
		private System.Windows.Forms.RadioButton radioButtonAlbedo;
		private System.Windows.Forms.RadioButton radioButtonDistance;
		private System.Windows.Forms.RadioButton radioButtonSetIndex;
		private System.Windows.Forms.RadioButton radioButtonSetColor;
		private System.Windows.Forms.RadioButton radioButtonNormal;
		private System.Windows.Forms.RadioButton radioButtonSetDistance;
		private System.Windows.Forms.RadioButton radioButtonSetNormal;
		private System.Windows.Forms.TextBox textBoxResults;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlAlbedo;
		private System.Windows.Forms.Label label1;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlNormal;
		private System.Windows.Forms.Label label2;
		private Nuaj.Cirrus.Utility.FloatTrackbarControl floatTrackbarControlPosition;
		private System.Windows.Forms.Label label3;
	}
}

