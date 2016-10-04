using System;
using System.Drawing;
using System.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.AudioVideoPlayback;

namespace GameEngine
{
	/// <summary>
	/// Summary description for Jukebox.
	/// </summary>
	public class Jukebox : IDisposable
	{
		#region Attributes
		private ArrayList  playlist = null;
		private int        current_song = 0;
		private int        volume = 0;
		#endregion

		#region Properties
		public float Volume { set { volume = (int)(-4000 * (1.0f - value)); } }
		#endregion

		/// <summary>
		/// Jukebox constructor
		/// </summary>
		public Jukebox( ) 
		{
			playlist = new ArrayList();
		}

		public void AddSong( string filename )
		{
			try
			{
				Music song = new Music(filename);
				song.Ending += new System.EventHandler(this.ClipEnded);
				playlist.Add(song);
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to add " + filename + " to the jukebox playlist ");
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to add " + filename + " to the jukebox playlist ");
				Console.AddLine(e.Message);
			}
		}

		public void Play()
		{
			if ( current_song < playlist.Count )
			{
				Music song = (Music)(playlist[current_song]);
				song.Ending += new System.EventHandler(this.ClipEnded);
				song.Volume = volume;
				song.Play();
			}
		}

		public void Stop()
		{
			Next();
		}

		public void Next()
		{
			Music song = (Music)(playlist[current_song]);
			song.Stop();
			song.SeekCurrentPosition(0.0, SeekPositionFlags.AbsolutePositioning );
			current_song++;
			if ( current_song >= playlist.Count )
			{
				current_song = 0;
			}
		}

		private void ClipEnded(object sender, System.EventArgs e)
		{
			Next();
			Play();
		}

		public void Dispose()
		{
			foreach ( Music song in playlist )
			{
				song.Dispose();
			}
		}

	}
}
