using System;
using System.Diagnostics;
using Microsoft.DirectX;
using GameEngine;
using GameAI;
using VehicleDynamics;
using Microsoft.DirectX.DirectInput;

namespace SampleGame
{
	/// <summary>
	/// Summary description for Ownship.
	/// </summary>
	public class Ownship : Car
	{
		#region Attributes
		private float ownship_speed = 0.0f;
		private float wheel = 0.0f;
		private float gas = 0.0f;
		private Listener ears = null;
		private SoundEffect engine_sound = null;
		private SoundEffect thump = null;
		private SoundEffect crash = null;
		private bool first_pass = true;
		private bool m_bUsingJoystick = false;
		private bool m_bUsingMouse = false;
		private bool m_bUsingKeyboard = false;
		#endregion

		#region Properties
		public bool UseJoystick { set { m_bUsingJoystick = value; } }
		public bool UseMouse { set { m_bUsingMouse = value; } }
		public bool UseKeyboard { set { m_bUsingKeyboard = value; } }
		#endregion

		public Ownship(System.Windows.Forms.Form form, string name, string meshFile, Vector3 offset, Attitude adjust ) 
			: base (name, meshFile, offset, adjust)
		{
			ears = new Listener(form, this);
			engine_sound = new SoundEffect(@"..\..\Resources\car_idle.wav");
			engine_sound.Looping = true;
			engine_sound.MinFreq = 9700;
			engine_sound.MaxFreq = 13500;
			thump = new SoundEffect(@"..\..\Resources\thump.wav");
			crash = new SoundEffect(@"..\..\Resources\crash.wav");
		}
		public override void Update( float DeltaT )
		{
			float delta_x_velocity;
			float delta_y_velocity;
			float delta_z_velocity;
			float delta_x_position;
			float delta_y_position;
			float delta_z_position;

			if ( first_pass )
			{
				first_pass = false;

				engine_sound.PlaySound();
			}

			ears.Update();

//			System.Diagnostics.Debug.WriteLine("ownship update");
//			North = North + ownship_speed * (float)Math.Cos(Heading) * DeltaT;
//			East  = East + ownship_speed * (float)Math.Sin(Heading) * DeltaT;

			if ( m_bUsingJoystick )
			{
				Steering = (CGameEngine.Inputs.GetJoystickNormalX()-1.0f);
				gas = (1.0f - (float)CGameEngine.Inputs.GetJoystickNormalY());
			}
			else if ( m_bUsingMouse )
			{
				try
				{
					gas += (float)CGameEngine.Inputs.GetMouseZ() * 0.1f;
					if ( CGameEngine.Inputs.IsKeyPressed(Key.DownArrow) )
					{
						gas -= 0.1f;
					}
					else if ( CGameEngine.Inputs.IsKeyPressed(Key.UpArrow) )
					{
						gas += 0.1f;
					}
					if ( gas > 1.0 )
					{
						gas = 1.0f;
					}
					else if ( gas < -1.0f )
					{
						gas = -1.0f;
					}
					float x = (float)CGameEngine.Inputs.GetMouseX();
					wheel += (x * .10f)*DeltaT;
					if ( wheel > 1.0 )
					{
						wheel = 1.0f;
					}
					else if ( wheel < -1.0f )
					{
						wheel = -1.0f;
					}

					Steering = wheel;
				}
				catch ( Exception e )
				{
					GameEngine.Console.AddLine("Exception");
					GameEngine.Console.AddLine(e.Message);
				}
			}
			else if ( m_bUsingKeyboard )
			{
				if ( CGameEngine.Inputs.IsKeyPressed(Key.LeftArrow) )
				{
					Heading = Heading - .50f * DeltaT;
				}
				else if ( CGameEngine.Inputs.IsKeyPressed(Key.RightArrow) )
				{
					Heading = Heading + .50f * DeltaT;
				}
				if ( CGameEngine.Inputs.IsKeyPressed(Key.DownArrow) )
				{
					ownship_speed -= 0.1f;
				}
				else if ( CGameEngine.Inputs.IsKeyPressed(Key.UpArrow) )
				{
					ownship_speed += 0.1f;
				}
			}

			if ( gas >= 0.0f ) 
			{
				Gas = gas;
				Brake = 0.0f;
			}
			if ( gas <= 0.0f )
			{
				Gas = 0.0f;
				Brake = -gas;
			}
//			GameEngine.Console.AddLine("Steering " + wheel + " gas " + gas );
			engine_sound.Frequency = Gas;
			engine_sound.Update();

			foreach ( Object3D test_obj in CGameApplication.Engine.Objects )
			{
				if ( test_obj !=  this )
				{
					if ( Collide(test_obj) )
					{
						delta_x_velocity = test_obj.VelocityX - VelocityX;
						delta_y_velocity = test_obj.VelocityY - VelocityY;
						delta_z_velocity = test_obj.VelocityZ - VelocityZ;
						delta_x_position= test_obj.Position.X;
						delta_y_position= test_obj.Position.X;
						delta_z_position= test_obj.Position.X;
						//						GameEngine.Console.AddLine( Name + " collided with " + test_obj.Name);
						if ( test_obj.Name.Substring(0,3) == "red"  ||
							test_obj.Name.Substring(0,4) == "blue" ||
							test_obj.Name.Substring(0,6) == "cactus" )
						{
							thump.PlaySound();
							Dynamics.MinorCollision();
						}
						else
						{
							crash.PlaySound();
							Dynamics.MajorCollision(delta_x_velocity, delta_y_velocity, delta_z_velocity, delta_x_position, delta_y_position, delta_z_position);
						}
					}
				}
			}
			foreach ( Object3D test_obj in GameEngine.BillBoard.Objects )
			{
				if ( test_obj !=  this )
				{
					if ( Collide(test_obj) )
					{
						delta_x_velocity = test_obj.VelocityX - VelocityX;
						delta_y_velocity = test_obj.VelocityY - VelocityY;
						delta_z_velocity = test_obj.VelocityZ - VelocityZ;
						delta_x_position= test_obj.Position.X;
						delta_y_position= test_obj.Position.X;
						delta_z_position= test_obj.Position.X;
						//						GameEngine.Console.AddLine( Name + " collided with " + test_obj.Name);
						if ( test_obj.Name.Substring(0,3) == "red"  ||
							 test_obj.Name.Substring(0,4) == "blue" ||
							 test_obj.Name.Substring(0,6) == "cactus" )
						{
							thump.PlaySound();
							Dynamics.MinorCollision();
						}
						else
						{
							crash.PlaySound();
							Dynamics.MajorCollision(delta_x_velocity, delta_y_velocity, delta_z_velocity, delta_x_position, delta_y_position, delta_z_position);
						}
					}
				}
			}
			base.Update( DeltaT );
//			Height = CGameEngine.Ground.HeightOfTerrain(Position) + Offset.Y;
			Height = (float)Dynamics.Height;
//			GameEngine.Console.AddLine( " height " + Height);
//			Attitude = CGameEngine.Ground.GetSlope(Position, Heading );
		}

		public override void Dispose()
		{
			Debug.WriteLine(" disposing of " + Name + " in Ownship");
			base.Dispose();
		}

	}
}
