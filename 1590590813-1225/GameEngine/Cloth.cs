using System;
using System.Collections;
using System.Drawing;
using System.Threading;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	/// <summary>
	/// Summary description for Model.
	/// </summary>
	public class Cloth : Object3D, IDynamic
	{
		private struct Node
		{
			public float  mass;
			public float  inverse_mass;
			public Vector3 position;
			public Vector3 velocity;
			public Vector3 acceleration;
			public Vector3 force;
			public bool    constrained;

			public Node(float mass_, double x, double y, double z, bool fixed_in_place)
			{
				mass = mass_;
				inverse_mass = 1.0f / mass;
				position.X = (float)x;
				position.Y = (float)y;
				position.Z = (float)z;
				velocity.X = 0.0f;
				velocity.Y = 0.0f;
				velocity.Z = 0.0f;
				acceleration.X = 0.0f;
				acceleration.Y = 0.0f;
				acceleration.Z = 0.0f;
				force.X = 0.0f;
				force.Y = 0.0f;
				force.Z = 0.0f;
				constrained = fixed_in_place;
			}
		}

		private struct NodeIndex
		{
			public int row;
			public int column;
		}

		private struct Spring
		{
			public NodeIndex node1;
			public NodeIndex node2;
			public float	 spring_constant;
			public float     damping;
			public float     length;
		}

		#region Attributes
		private CustomVertex.PositionNormalTextured[]  m_vertices;   
		private VertexBuffer m_VB = null;  // Vertex buffer
		private IndexBuffer  m_IB = null;  // Index buffer
		//indices buffer
		private short[] indices;
		private Texture      m_Texture; // image for face
		private Node[,] nodes;
		private int num_springs;
		private int num_faces;
		private int num_rows;
		private int num_columns;
		private int num_nodes;
		private Spring[] springs;
		private Vector3  m_vOffset = new Vector3(0.0f, 0.0f, 0.0f);
		private Attitude m_AttitudeOffset = new Attitude();
		private bool m_bValid = false;
		private Thread        m_physics_thread;
		private bool thread_active = true;
		private Mutex mutex = new Mutex();

		// global variables
		private static Vector3 wind = new Vector3();

		// constants
		private static float gravity = -32.174f; 
		private static float spring_tension = 50.10f;
		private static float damping = 1.70f;
		private static float drag_coefficient = 0.01f;
		#endregion

		#region Properties
		public Vector3 Offset { set { m_vOffset = value; } get { return m_vOffset; } }

		public static float EastWind { set { wind.X = value; } get { return wind.X; } }
		public static float NorthWind { set { wind.Z = value; } get { return wind.Z; } }
		#endregion


		public Cloth(string name, string texture_name, int rows, int columns, double spacing, float mass) : base(name)
		{
			try 
			{
				num_rows = rows;
				num_columns = columns;
				m_Texture = GraphicsUtility.CreateTexture(CGameEngine.Device3D, texture_name); 
				nodes = new Node[rows+1,columns+1];
				num_faces = rows * columns * 2;
				num_springs = columns * ( rows+1) + rows * (columns+1) + columns*rows*2;
				springs = new Spring[num_springs];

				wind.X = 0.0f;
				wind.Y = 0.0f;
				wind.Z = 0.0f;

				num_nodes = (rows+1) * (columns+1);

				m_vertices = new CustomVertex.PositionNormalTextured[num_nodes];

				float mass_per_node = mass / num_nodes;

				for ( int r=0; r<=rows; r++ )
				{
					for ( int c=0; c <=columns; c++ )
					{
						nodes[r,c] = new Node(mass_per_node, -(c*spacing), -(r*spacing), 0.0, c==0 && (r==0 || r == rows));
					}
				}

				// Create a buffer for rendering the cloth
				m_VB = new VertexBuffer( typeof(CustomVertex.PositionNormalTextured), num_nodes,
					CGameEngine.Device3D, Usage.WriteOnly, CustomVertex.PositionNormalTextured.Format,
					Pool.Default );

				m_IB = new IndexBuffer(typeof(short), num_faces * 3, CGameEngine.Device3D, Usage.WriteOnly, Pool.Managed);
				indices = new short[num_faces * 3];
				m_IB.Created += new System.EventHandler(this.PopulateIndexBuffer);
				this.PopulateIndexBuffer(m_IB, null);

				m_VB.Created += new System.EventHandler(this.PopulateBuffer);
				this.PopulateBuffer(m_VB, null);

				// create the springs
				int index = 0;
				for ( int r=0; r<=rows; r++ )
				{
					for ( int c=0; c <=columns; c++ )
					{
						if ( c < columns )
						{
							springs[index].node1.row = r;
							springs[index].node1.column = c;
							springs[index].node2.row = r;
							springs[index].node2.column = c+1;
							springs[index].spring_constant = spring_tension;
							springs[index].damping = damping;
							Vector3 length = nodes[r,c].position - nodes[r,c+1].position;
							springs[index].length = length.Length();
							index++;
						}
						if ( r < rows )
						{
							springs[index].node1.row = r;
							springs[index].node1.column = c;
							springs[index].node2.row = r+1;
							springs[index].node2.column = c;
							springs[index].spring_constant = spring_tension;
							springs[index].damping = damping;
							Vector3 length = nodes[r,c].position - nodes[r+1,c].position;
							springs[index].length = length.Length();
							index++;
						}
						if ( r < (rows-1) && c < (columns-1) )
						{
							springs[index].node1.row = r;
							springs[index].node1.column = c;
							springs[index].node2.row = r+1;
							springs[index].node2.column = c+1;
							springs[index].spring_constant = spring_tension;
							springs[index].damping = damping;
							Vector3 length = nodes[r,c].position - nodes[r+1,c+1].position;
							springs[index].length = length.Length();
							index++;
						}
						if ( r < (rows-1) && c > 0 )
						{
							springs[index].node1.row = r;
							springs[index].node1.column = c;
							springs[index].node2.row = r+1;
							springs[index].node2.column = c-1;
							springs[index].spring_constant = spring_tension;
							springs[index].damping = damping;
							Vector3 length = nodes[r,c].position - nodes[r+1,c-1].position;
							springs[index].length = length.Length();
							index++;
						}
					}
				}

				m_physics_thread = new Thread(new ThreadStart(DoPhysics));
				m_physics_thread.Start();
				m_bValid = true;
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to create cloth for " + name);
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to create cloth for " + name);
				Console.AddLine(e.Message);
			}
			
		}

		public void PopulateBuffer(object sender, EventArgs e)
		{
			VertexBuffer vb = (VertexBuffer)sender;

			int index = 0;
			for ( int r=0; r<(1+num_rows); r++ )
			{
				for ( int c=0; c <(1+num_columns); c++ )
				{
					m_vertices[index].SetPosition(nodes[r,c].position);
					m_vertices[index].SetNormal(new Vector3(0.0f, 0.0f, 1.0f));
					m_vertices[index].Tv = (float)r / (float)(num_rows);
					m_vertices[index].Tu = (float)c / (float)(num_columns);
					index++;
				}
			}
			// Copy vertices into vertexbuffer
			mutex.WaitOne();
			vb.SetData(m_vertices, 0, 0);
			mutex.ReleaseMutex();
		}

		public void PopulateIndexBuffer(object sender, EventArgs e)
		{
			int index = 0;

			IndexBuffer g = (IndexBuffer)sender;
			for ( int r=0; r<num_rows; r++)
			{
				for ( int c=0; c<num_columns; c++)
				{
					indices[index]   = (short)((r)  *(1+num_columns) + (c));
					indices[index+1] = (short)((r+1)*(1+num_columns) + (c));
					indices[index+2] = (short)((r)  *(1+num_columns) + (c+1));

					indices[index+3] = (short)((r)  *(1+num_columns) + (c+1));
					indices[index+4] = (short)((r+1)*(1+num_columns) + (c));
					indices[index+5] = (short)((r+1)*(1+num_columns) + (c+1));
					index += 6;
				}
			}

			mutex.WaitOne();
			g.SetData(indices, 0, 0);
			mutex.ReleaseMutex();
		}

		public override bool Collide( Object3D Other ) 
		{ 
			return false; 
		}

		public override void Render( Camera cam )
		{
			if ( m_bValid ) 
			{
				Matrix world_matrix;

				if ( m_Parent != null )
				{
					world_matrix = Matrix.Multiply(m_Matrix, m_Parent.WorldMatrix);
				}
				else
				{
					world_matrix = m_Matrix;
				}

				CGameEngine.Device3D.Transform.World = world_matrix;

				Material mtrl = new Material();
				mtrl.Ambient = Color.White;
				mtrl.Diffuse = Color.White;
				CGameEngine.Device3D.Material = mtrl;
				CGameEngine.Device3D.SetStreamSource( 0, m_VB, 0 );
				CGameEngine.Device3D.VertexFormat = CustomVertex.PositionNormalTextured.Format;

				CGameEngine.Device3D.RenderState.CullMode = Cull.None;

				// Set the texture
				CGameEngine.Device3D.SetTexture(0, m_Texture );

				// set the indices			
				CGameEngine.Device3D.Indices = m_IB;

				// Render the face
				mutex.WaitOne();
				CGameEngine.Device3D.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, num_nodes, 0, num_faces);
				mutex.ReleaseMutex();

				if ( m_Children.Count > 0 )
				{
					Object3D obj;
					for ( int i=0; i<m_Children.Count; i++ )
					{
						obj = (Object3D)m_Children.GetByIndex(i);
						obj.Render( cam );
					}
				}
			}
		}

		public override void Update( float DeltaT )
		{
			if ( m_UpdateMethod != null ) 
			{
				m_UpdateMethod( (Object3D)this, DeltaT );
			}
			if ( m_Children.Count > 0 )
			{
				Object3D obj;
				for ( int i=0; i<m_Children.Count; i++ )
				{
					obj = (Object3D)m_Children.GetByIndex(i);
					obj.Update( DeltaT );
				}
			}
		}

		private void DoPhysics()
		{
			Vector3 parent_wind = new Vector3(0.0f, 0.0f, 0.0f);
			Console.AddLine("cloth physics thread started");
			System.Random rand = new System.Random();
			while ( thread_active )
			{

				try
				{
					if ( m_Parent != null )
					{
//						parent_wind = m_Parent.Velocity * -0.10f;
					}
					m_Matrix = Matrix.Identity;
					m_Matrix = Matrix.RotationYawPitchRoll(Heading+m_AttitudeOffset.Heading,
						Pitch+m_AttitudeOffset.Pitch,Roll+m_AttitudeOffset.Roll);
					Matrix temp = Matrix.Translation(m_vPosition);
					m_Matrix.Multiply(temp);

					for ( int r=0; r<=num_rows; r++ )
					{
						for ( int c=0; c <=num_columns; c++ )
						{
							nodes[r,c].force.X = 0.0f;
							nodes[r,c].force.Y = 0.0f;
							nodes[r,c].force.Z = 0.0f;
						}
					}

					// process external forces
					for ( int r=0; r<=num_rows; r++ )
					{
						for ( int c=0; c <=num_columns; c++ )
						{
							if ( !nodes[r,c].constrained )
							{
								// gravity
								nodes[r,c].force.Y += (float)(gravity * nodes[r,c].mass);

								// drag
								Vector3 drag = nodes[r,c].velocity;
								drag.Multiply(-1.0f);
								drag.Normalize();
								drag.Multiply((nodes[r,c].velocity.Length() * nodes[r,c].velocity.Length()) * drag_coefficient);
								nodes[r,c].force += drag;

								// wind
								Vector3 turbulence = new Vector3((float)rand.NextDouble(), 0.0f, (float)rand.NextDouble());
								Vector3 total_wind = wind + turbulence + parent_wind;
								nodes[r,c].force += total_wind;
								if ( r==1 && c==1 )
								{
//									Console.AddLine(" wind " + total_wind.X + " " + total_wind.Y + " " +total_wind.Z );
								}
							}
						}
					}

					// spring forces
					for ( int i=0; i<num_springs; i++ )
					{
						int row1    = springs[i].node1.row;
						int column1 = springs[i].node1.column;
						int row2    = springs[i].node2.row;
						int column2 = springs[i].node2.column;

						Vector3 distance = nodes[row1,column1].position - nodes[row2,column2].position;
						float spring_length = distance.Length();
						Vector3 normalized_distance = distance;
						normalized_distance.Multiply( 1.0f / spring_length );
						Vector3 velocity = nodes[row1,column1].velocity - nodes[row2,column2].velocity;
						float length = springs[i].length;

						float spring_force = springs[i].spring_constant * (spring_length - length);
						float damping_force = springs[i].damping * Vector3.Dot(velocity,distance) / spring_length;

						Vector3 force2 = (spring_force + damping_force) * normalized_distance;
						Vector3 force1 = force2;
						force1.Multiply(-1.0f);

						if ( !nodes[row1,column1].constrained )
						{
							nodes[row1,column1].force += force1;
						}

						if ( !nodes[row2,column2].constrained )
						{
							nodes[row2,column2].force += force2;
						}
					}

					//				DeltaT *= 0.001f;

					// integrate position
					for ( int r=0; r<=num_rows; r++ )
					{
						for ( int c=0; c <=num_columns; c++ )
						{
							float x;
							Vector3 accel = nodes[r,c].force;
							accel.Multiply( nodes[r,c].inverse_mass);
							nodes[r,c].acceleration = accel;
							nodes[r,c].velocity.X += accel.X * 0.01f;
							nodes[r,c].velocity.Y += accel.Y * 0.01f;
							nodes[r,c].velocity.Z += accel.Z * 0.01f;
							nodes[r,c].position.X += nodes[r,c].velocity.X * 0.01f;
							nodes[r,c].position.Y += nodes[r,c].velocity.Y * 0.01f;
							nodes[r,c].position.Z += nodes[r,c].velocity.Z * 0.01f;
							x=1;
							if ( r==1 && c==1 )
							{
//								Console.AddLine("Force " + accel.X + " " + accel.Y + " " + accel.Z);
//								Console.AddLine("velocity " + nodes[r,c].velocity.X + " " + nodes[r,c].velocity.Y + " " + nodes[r,c].velocity.Z);
//								Console.AddLine("position " + nodes[r,c].position.X + " " + nodes[r,c].position.Y + " " + nodes[r,c].position.Z);
//								Console.AddLine("");
							}
						}
					}
					PopulateBuffer((object)m_VB, null);

				}
				catch (DirectXException d3de)
				{
					Console.AddLine("Unable to update cloth " + Name);
					Console.AddLine(d3de.ErrorString);
				}
				catch ( Exception e )
				{
					Console.AddLine("Unable to update cloth " + Name);
					Console.AddLine(e.Message);
				}

				Thread.Sleep(10);

			}
			Console.AddLine("cloth physics thread terminated");
		}

		public override void Dispose()
		{
			thread_active = false;
			if ( m_VB != null ) 
			{
				m_VB.Dispose();
				m_IB.Dispose();
				m_Texture.Dispose();
			}
			Thread.Sleep(100);
		}
	}
}
