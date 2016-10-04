using System;
using Microsoft.DirectX;

namespace GameEngine
{
	/// <summary> Basic structures used in the game</summary>
	public struct Attitude
	{
		public float Pitch;
		public float Heading;
		public float Roll;

		public Attitude( float pitch, float heading, float roll )
		{
			Pitch = pitch;
			Heading = heading;
			Roll = roll;
		}
		public static float Aepc( float angle )
		{
			if ( angle > (2.0*Math.PI) )
			{
				angle -= (float)(2.0*Math.PI);
			}
			if ( angle < 0.0f )
			{
				angle += (float)(2.0*Math.PI);
			}
			return angle;
		}
		public static float Aepc2( float angle )
		{
			if ( angle > Math.PI )
			{
				angle -= (float)(2.0*Math.PI);
			}
			if ( angle < -Math.PI )
			{
				angle += (float)(2.0*Math.PI);
			}
			return angle;
		}
	}

	/// <summary>
	/// Interfaces used in the game engine.
	/// </summary>
	public interface IRenderable
	{
		void Render(Camera cam);
	}

	public interface ICullable
	{
		bool Culled { set; }
		bool IsCulled { get; }
	}

	public interface ICollidable
	{
		Vector3 CenterOfMass { get; }
		float   BoundingRadius { get; }
		bool CollideSphere ( Object3D other );
		bool CollidePolygon ( Vector3 Point1, Vector3 Point2, Vector3 Point3 );
	}

	public interface IDynamic
	{
		void Update( float DeltaT );
	}

	public interface ITerrainInfo
	{
		float    HeightOfTerrain( Vector3 Position );
		float    HeightAboveTerrain( Vector3 Position );
		bool     InLineOfSight( Vector3 Position1, Vector3 Position2 );
		Attitude GetSlope( Vector3 Position, float Heading );
	}
}
