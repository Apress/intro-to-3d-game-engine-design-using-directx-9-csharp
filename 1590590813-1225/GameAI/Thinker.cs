using System;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using System.Xml;
using GameEngine;

namespace GameAI
{
	/// <summary>
	/// Summary description for Thinker.
	/// </summary>
	public class Thinker : IDisposable
	{
		#region delegates
		public delegate void SensorMethod( Thinker the_thinker );
		public delegate void ActionMethod( Thinker the_thinker );
		#endregion

		#region Attributes
		private ArrayList     m_state_list = null;
		private ArrayList     m_sensor_methods = null;
		private AIState       m_current_state = null;
		private SortedList    m_fact_list;
		private Thread        m_think_thread;
		private Model         m_model = null;
		private bool          m_thread_active = true;

		private static SortedList m_methods = new SortedList();
		#endregion

		#region Properties
		public Model Self { get { return m_model; } }
		#endregion

		public Thinker( Model model )
		{
			m_model = model;
			m_state_list = new ArrayList();
			m_sensor_methods = new ArrayList();
			m_fact_list = new SortedList();

			m_methods = new SortedList();

			m_think_thread = new Thread(new ThreadStart(Execute));
			m_think_thread.IsBackground = true;
			m_think_thread.Start();
		}

		public static void AddAction( string action_name, ActionMethod method )
		{
			if ( !m_methods.Contains( action_name ) )
			{
				m_methods.Add(action_name, method);
			}
		}

		public static ActionMethod GetAction( string action_name )
		{
			ActionMethod method = null;

			if ( m_methods.Contains( action_name ) )
			{
				int index = m_methods.IndexOfKey( action_name );
				method = (ActionMethod)m_methods.GetByIndex(index);
			}

			return method;
		}

		public static string GetActionName( ActionMethod method )
		{
			string action_name = null;

			if ( m_methods.Contains( method ) )
			{
				int index = m_methods.IndexOfValue( method );
				action_name = (string)m_methods.GetKey(index);
			}

			return action_name;
		}

		public void Execute()
		{
			Debug.WriteLine("thinker thread started");
			while ( m_thread_active )
			{
				if ( m_current_state != null )
				{
					foreach ( SensorMethod method in m_sensor_methods )
					{
						method( this );
					}

					m_current_state.DoActions( this );
					m_current_state = m_current_state.Think();
				}
			}
			Debug.WriteLine("thinker thread terminated");

		}

		public void Dispose()
		{
			m_thread_active = false;

			while ( m_think_thread.IsAlive ) Thread.Sleep(1);
		}

		public AIState GetState( string name )
		{
			AIState the_state = null;

			foreach ( AIState state in m_state_list )
			{
				if ( state.Name == name )
				{
					the_state = state;
				}
			}
			return the_state;
		}

		public Fact GetFact( string name )
		{
			Fact the_fact = null;

			if ( m_fact_list.Contains( name ) )
			{
				int index = m_fact_list.IndexOfKey( name );
				the_fact = (Fact)m_fact_list.GetByIndex(index);
			}
			else
			{
				the_fact = new Fact(name);
				the_fact.Value = 0.0f;
				m_fact_list.Add( name, the_fact );
			}
			return the_fact;
		}

		public void SetFact( string name, float value )
		{
			if ( m_fact_list.Contains( name ) )
			{
				int index = m_fact_list.IndexOfKey( name );
				Fact fact = (Fact)m_fact_list.GetByIndex(index);
				fact.Value = value;
			}
			else
			{
				Fact fact = new Fact(name);
				fact.Value = value;
				m_fact_list.Add( name, fact );
			}
		}

		public void AddSensorMethod( SensorMethod method )
		{
			m_sensor_methods.Add( method );
		}

		public void AddState( AIState state )
		{
			m_state_list.Add( state );
		}

		public void Write( string filename )
		{
			XmlTextWriter writer = new XmlTextWriter( filename, null );

			writer.WriteStartDocument();
			writer.WriteStartElement("Knowledge");

			//Use indentation for readability.
			writer.Formatting = Formatting.Indented;
			writer.Indentation = 4;
        
			int num_facts = m_fact_list.Count;

			for ( int i=0; i<num_facts; i++ )
			{
				Fact fact = (Fact)m_fact_list.GetByIndex(i);
				fact.Write( writer );
			}

			foreach ( AIState state in m_state_list )
			{
				state.WriteStateName( writer );
			}

			foreach ( AIState state in m_state_list )
			{
				state.WriteFullState( writer, this );
			}

			writer.WriteEndElement();
			writer.WriteEndDocument();
			writer.Close();
		}

		public void Read( string filename )
		{
			XmlTextReader reader = new XmlTextReader( filename );
			string name = "unknown";
			float float_value;
			AIState state = null;

			try
			{

				reader.Read();
				// If the node has value
				while ( reader.Read() ) 
				{
					// Process a start of element node.
					if (reader.NodeType == XmlNodeType.Element) 
					{ 
						// Process a text node.
						if ( reader.Name == "name" ) 
						{
							while (reader.NodeType != XmlNodeType.Text) 
							{
								reader.Read();
							}
							name = reader.Value;
						}
						else if ( reader.Name == "Value" )
						{
							while (reader.NodeType != XmlNodeType.Text) 
							{
								reader.Read();
							}
							float_value = float.Parse(reader.Value);
							SetFact(name, float_value);
						}
						else if ( reader.Name == "StateName" )
						{
							while (reader.NodeType != XmlNodeType.Text) 
							{
								reader.Read();
							}
							state = new AIState(reader.Value);
							AddState( state );
						}
						else if ( reader.Name == "StateDefinition" )
						{
							while (reader.NodeType != XmlNodeType.Text) 
							{
								reader.Read();
							}
							state = GetState(reader.Value);
							state.Read( reader, this );
						}
					}
				}// End while loop

				if ( m_state_list.Count != 0 )
				{
					m_current_state = (AIState)m_state_list[0];
				}
				reader.Close();
			}
			catch ( Exception e )
			{
				System.Diagnostics.Debug.WriteLine("error in thinker read method/n");
				System.Diagnostics.Debug.WriteLine(e.Message);
			}
		}
	}
}
