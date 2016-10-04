using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using Sound = Microsoft.DirectX.DirectSound;
using Buffer = Microsoft.DirectX.DirectSound.Buffer;

namespace GameEngine
{
	/// <summary>
	/// Summary description for Listener.
	/// </summary>
	public class Listener : IDisposable
	{

		#region Attributes
		private Sound.Listener3DSettings listenerParameters = new Sound.Listener3DSettings();
		private Sound.Listener3D applicationListener = null;
		private Object3D m_listener = null;

		private static Sound.Device applicationDevice = new Sound.Device();
		#endregion

		#region Properties
		public static Sound.Device Device { get { return applicationDevice; } }
		#endregion

		public Listener(System.Windows.Forms.Form form, Object3D object_listening)
		{
			m_listener = object_listening;
			Sound.BufferDescription description = new Sound.BufferDescription();
			Sound.WaveFormat fmt = new Sound.WaveFormat();
			description.PrimaryBuffer = true;
			description.Control3D = true;
			Sound.Buffer buff	= null;
		
			fmt.FormatTag = Sound.WaveFormatTag.Pcm;
			fmt.Channels = 2;
			fmt.SamplesPerSecond = 22050;
			fmt.BitsPerSample = 16;
			fmt.BlockAlign = (short)(fmt.BitsPerSample / 8 * fmt.Channels);
			fmt.AverageBytesPerSecond = fmt.SamplesPerSecond * fmt.BlockAlign;

			applicationDevice.SetCooperativeLevel( form, Sound.CooperativeLevel.Priority);
		
			// Get the primary buffer and set the format.
			buff = new Buffer(description, Device);
			buff.Format = fmt;

			applicationListener = new Listener3D(buff);
			listenerParameters = applicationListener.AllParameters;
		}

		public void Update()
		{
			if ( m_listener != null )
			{
				listenerParameters.Position = m_listener.Position;
				
				Vector3 front = new Vector3( 0.0f, 0.0f, 1.0f );
				Vector3 top   = new Vector3( 0.0f, 1.0f, 0.0f );

				Matrix transform = Matrix.RotationYawPitchRoll(
					m_listener.Attitude.Heading,
					m_listener.Attitude.Pitch, 
					m_listener.Attitude.Roll);

				listenerParameters.OrientFront = 
					Vector3.TransformCoordinate( front, transform );
				listenerParameters.OrientTop   = 
					Vector3.TransformCoordinate( top, transform );
			}
			applicationListener.CommitDeferredSettings();

		}

		public void Dispose()
		{
			applicationListener.Dispose();
			applicationDevice.Dispose();
		}
	}
}
