using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace SampleGame
{
	/// <summary>
	/// Summary description for SelectControls.
	/// </summary>
	public class SelectControls : System.Windows.Forms.Form
	{
		public System.Windows.Forms.RadioButton UseKeyboard;
		public System.Windows.Forms.RadioButton UseJoystick;
		public System.Windows.Forms.RadioButton UseMouse;
		private new System.Windows.Forms.Button Close;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SelectControls()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
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
			this.UseKeyboard = new System.Windows.Forms.RadioButton();
			this.UseJoystick = new System.Windows.Forms.RadioButton();
			this.UseMouse = new System.Windows.Forms.RadioButton();
			this.Close = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// UseKeyboard
			// 
			this.UseKeyboard.Location = new System.Drawing.Point(37, 38);
			this.UseKeyboard.Name = "UseKeyboard";
			this.UseKeyboard.Size = new System.Drawing.Size(95, 23);
			this.UseKeyboard.TabIndex = 0;
			this.UseKeyboard.Text = "Keyboard";
			// 
			// UseJoystick
			// 
			this.UseJoystick.Location = new System.Drawing.Point(37, 98);
			this.UseJoystick.Name = "UseJoystick";
			this.UseJoystick.Size = new System.Drawing.Size(95, 23);
			this.UseJoystick.TabIndex = 1;
			this.UseJoystick.Text = "Joystick";
			// 
			// UseMouse
			// 
			this.UseMouse.Checked = true;
			this.UseMouse.Location = new System.Drawing.Point(37, 68);
			this.UseMouse.Name = "UseMouse";
			this.UseMouse.Size = new System.Drawing.Size(95, 23);
			this.UseMouse.TabIndex = 2;
			this.UseMouse.TabStop = true;
			this.UseMouse.Text = "Mouse";
			// 
			// Close
			// 
			this.Close.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.Close.Location = new System.Drawing.Point(44, 151);
			this.Close.Name = "Close";
			this.Close.Size = new System.Drawing.Size(68, 22);
			this.Close.TabIndex = 3;
			this.Close.Text = "Close";
			this.Close.Click += new System.EventHandler(this.Close_Click);
			// 
			// SelectControls
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(211, 204);
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.Close,
																		  this.UseMouse,
																		  this.UseJoystick,
																		  this.UseKeyboard});
			this.Name = "SelectControls";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "SelectControls";
			this.ResumeLayout(false);

		}
		#endregion

		private void Close_Click(object sender, System.EventArgs e)
		{
			Dispose();
		}

	}
}
