using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using Sound = Microsoft.DirectX.DirectSound;
using Buffer = Microsoft.DirectX.DirectSound.Buffer;

namespace GameEngine
{
	/// <summary>
	/// Summary description for SoundEffect.
	/// </summary>
	public class SoundEffect : IDisposable
	{
		#region Attributes
		private SecondaryBuffer soundBuffer = null;
		private Buffer3D soundBuffer3D = null;
		private Object3D m_source = null;
		private string m_FileName;
		private bool looping = false;
		private int min_freq = 22050;
		private int max_freq = 22050;
		private float current_freq = 0.0f;

		private static int master_volume = 0;
		#endregion

		#region Properties
		public bool Looping { set { looping = value; } get { return looping; } }
		public int  MinFreq { set { min_freq = value; } get { return min_freq; } }
		public int  MaxFreq { set { max_freq = value; } get { return max_freq; } }
		public float Frequency { 
			set { 
				current_freq = value;
				if ( current_freq > 1.0 ) current_freq = 1.0f;
				if ( current_freq < 0.0 ) current_freq = 0.0f; 
			} get { return current_freq; } }
		public float  MinDistance { set { soundBuffer3D.MinDistance = value; } get { return soundBuffer3D.MinDistance; } }
		public float  MaxDistance { set { soundBuffer3D.MaxDistance = value; } get { return soundBuffer3D.MaxDistance; } }

		public static float Volume { set { master_volume = (int)(-4000 * (1.0f - value)); } }

		private static int MasterVolume { get { return master_volume; } }
		#endregion

		public SoundEffect( string FileName)
		{
			m_FileName = FileName;
			LoadSoundFile();

		}

		private void LoadSoundFile()
		{

			BufferDescription description = new BufferDescription();

			description.Guid3DAlgorithm = DSoundHelper.Guid3DAlgorithmHrtfLight;
			description.Control3D = true;
			description.ControlFrequency = true;
			description.ControlVolume = true;

			if (null != soundBuffer)
			{
				soundBuffer.Stop();
				soundBuffer.SetCurrentPosition(0);
			}

			// Load the wave file into a DirectSound buffer
			try
			{
				soundBuffer = new SecondaryBuffer(m_FileName, description, Listener.Device);
				soundBuffer3D = new Buffer3D(soundBuffer);
			}
			catch ( Exception e )
			{
				GameEngine.Console.AddLine("Exception on loading " + m_FileName + ". Ensure file is Mono");
				GameEngine.Console.AddLine(e.Message);
			}
		
			if (WaveFormatTag.Pcm != (WaveFormatTag.Pcm & description.Format.FormatTag))
			{
				GameEngine.Console.AddLine("Wave file must be PCM for 3D control.");
				if (null != soundBuffer)
					soundBuffer.Dispose();
				soundBuffer = null;
			}
		}

		private bool RestoreBuffer()
		{
			if (false == soundBuffer.Status.BufferLost)
				return false;

			while(true == soundBuffer.Status.BufferLost)
			{
				soundBuffer.Restore();
			}
			return true;
		}

		public void PlaySound()
		{
			try
			{
				BufferPlayFlags flags;
				if ( looping )
				{
					flags = BufferPlayFlags.Looping;
				}
				else
				{
					flags = BufferPlayFlags.Default;
				}

				if (RestoreBuffer())
				{
					LoadSoundFile();
					soundBuffer.SetCurrentPosition(0);
				}
				soundBuffer.Play(0, flags);
			}
			catch ( Exception e )
			{
				GameEngine.Console.AddLine("Exception on playing " + m_FileName);
				GameEngine.Console.AddLine(e.Message);
			}
		}

		public void StopSound()
		{
			try
			{
				soundBuffer.Stop();
				soundBuffer.SetCurrentPosition(0);
			}
			catch ( Exception e )
			{
				GameEngine.Console.AddLine("Exception on stopping " + m_FileName);
				GameEngine.Console.AddLine(e.Message);
			}
		}

		public void Update()
		{
			try
			{
				int freq_range = max_freq - min_freq;

				if ( freq_range > 0 )
				{
					soundBuffer.Frequency = 
						min_freq + (int)(freq_range * current_freq);
				}

				soundBuffer.Volume = MasterVolume;

				if ( m_source != null )
				{
					soundBuffer3D.Position = m_source.Position;
				}
			}
			catch ( Exception e )
			{
				GameEngine.Console.AddLine("Exception while updating " + m_FileName);
				GameEngine.Console.AddLine(e.Message);
			}
		}

		public void Dispose()
		{
			try
			{
				soundBuffer.Dispose();
				soundBuffer3D.Dispose();
			}
			catch 
			{
				// do nothing - if this failed there was probably nothing to dispose of
			}
		}
	}
}
