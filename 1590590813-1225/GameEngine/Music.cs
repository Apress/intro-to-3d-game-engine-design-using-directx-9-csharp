using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.AudioVideoPlayback;

namespace GameEngine
{
	/// <summary>
	/// Summary description for Music.
	/// </summary>
	public class Music : Microsoft.DirectX.AudioVideoPlayback.Audio
	{
		#region Attributes
		private bool  loop = false;
		#endregion

		#region Properties
		public bool  Loop { get { return loop; } set { loop = value; } }
		public float MusicVolume { set { base.Volume = (int)(-4000 * (1.0f - value)); } }
		#endregion

		/// <summary>
		/// Music constructor
		/// </summary>
		public Music( string filename ) : base(filename)
		{
			try
			{
				Ending += new System.EventHandler(this.ClipEnded);
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to create music ");
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to create music ");
				Console.AddLine(e.Message);
			}
		}

		private void ClipEnded(object sender, System.EventArgs e)
		{
			// The clip has ended, stop and restart it
			if ( loop )
			{
				Stop();
				Play();
			}
		}

	}
}
