using System;
using System.Xml;

namespace GameAI
{
	/// <summary>
	/// Summary description for Transitioner.
	/// </summary>
	public class Transitioner
	{
		#region Attributes
		private Expression m_expression = null;
		private AIState    m_target_state;
		#endregion

		#region Properties
		#endregion

		public Transitioner(Expression expression, AIState target_state)
		{
			m_expression = expression;
			m_target_state = target_state;
		}

		public AIState Evaluate( AIState old_state )
		{
			AIState new_state = old_state;

			if ( m_expression != null )
			{
				if ( m_expression.Evaluate() )
				{
					new_state = m_target_state;
				}
			}

			return new_state;
		}


		public void Write( XmlTextWriter writer )
		{
			writer.WriteStartElement("Transitioner");
			writer.WriteElementString("Target", m_target_state.Name);
			m_expression.Write( writer );
			writer.WriteEndElement();
		}

		public void Read ( XmlTextReader reader, Thinker thinker )
		{
			bool done = false;

			while ( !done ) 
			{
				reader.Read();

				if ( reader.NodeType == XmlNodeType.EndElement &&
					reader.Name == "Transitioner" )
				{
					done =true;
				}
					// Process a start of element node.
				else if (reader.NodeType == XmlNodeType.Element) 
				{ 
					// Process a text node.
					if ( reader.Name == "Expression" ) 
					{
						m_expression.Read( reader, thinker );
					}
				}
			}// End while loop
		}
	}
}
