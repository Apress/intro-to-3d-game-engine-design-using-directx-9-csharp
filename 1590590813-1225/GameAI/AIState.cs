using System;
using System.Collections;
using System.Xml;

namespace GameAI
{
	/// <summary>
	/// Summary description for AIState.
	/// </summary>
	public class AIState
	{
		#region Attributes
		private string m_name;
		private ArrayList m_transition_list;
		private ArrayList m_actions;
		#endregion

		#region Properties
		public string Name { get { return m_name; } }
		#endregion

		public AIState(string name)
		{
			m_name = name;
			m_transition_list = new ArrayList();
			m_actions = new ArrayList();
		}

		public void AddAction( Thinker.ActionMethod method )
		{
			m_actions.Add( method );
		}

		public void DoActions( Thinker thinker )
		{
			foreach ( Thinker.ActionMethod act in m_actions )
			{
				act(thinker);
			}
		}

		public AIState Think()
		{
			AIState new_state;

			foreach ( Transitioner trans in m_transition_list )
			{
				new_state = trans.Evaluate( this );
				if ( new_state != this )
				{
					return new_state;
				}
			}
			return this;
		}

		public void AddTransitioner ( Transitioner trans )
		{
			m_transition_list.Add( trans );
		}

		public void WriteStateName( XmlTextWriter writer )
		{
			writer.WriteStartElement("StateName");
			writer.WriteElementString("name", m_name);
			writer.WriteEndElement();
		}

		public void WriteFullState( XmlTextWriter writer, Thinker thinker )
		{
			writer.WriteStartElement("StateDefinition");
			writer.WriteElementString("name", m_name);
			foreach ( Transitioner trans in m_transition_list )
			{
				trans.Write( writer );
			}
			foreach ( Thinker.ActionMethod act in m_actions )
			{
				writer.WriteStartElement("StateAction");
				writer.WriteElementString("name", Thinker.GetActionName( act ));
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		public void Read ( XmlTextReader reader, Thinker thinker )
		{
			bool done = false;
			Transitioner trans = null;
			Expression exp = null;

			while ( !done ) 
			{
				reader.Read();

				if ( reader.NodeType == XmlNodeType.EndElement &&
					 reader.Name == "StateDefinition" )
				{
					done =true;
				}
				// Process a start of element node.
				else if (reader.NodeType == XmlNodeType.Element) 
				{ 
					// Process a text node.
					if ( reader.Name == "Target" )
					{
						while (reader.NodeType != XmlNodeType.Text) 
						{
							reader.Read();
						}
						AIState state = thinker.GetState(reader.Value);
						exp = new Expression();
						trans = new Transitioner( exp, state );
						AddTransitioner( trans );
						trans.Read( reader, thinker );
					}
					if ( reader.Name == "StateAction" )
					{
						while (reader.NodeType != XmlNodeType.Text) 
						{
							reader.Read();
						}
						Thinker.ActionMethod method = Thinker.GetAction(reader.Value);
						m_actions.Add( method );
					}
				}
			}// End while loop
		}
	}
}
