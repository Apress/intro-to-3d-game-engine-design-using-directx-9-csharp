using System;
using System.Collections;
using System.Xml;

namespace GameAI
{
	/// <summary>
	/// Summary description for Expression.
	/// </summary>
	public class Expression
	{
		#region Attributes
		private ArrayList m_logic_list;
		private bool m_and_values = true;
		#endregion

		#region Properties
		public bool CombineByAnding { set { m_and_values = value; } }
		#endregion

		public Expression()
		{
			m_logic_list = new ArrayList();
		}

		public void Clear()
		{
			m_logic_list.Clear();
		}

		public void AddLogic( Logic logic )
		{
			m_logic_list.Add(logic);
		}

		public bool Evaluate()
		{
			bool result = false;
			bool first_logic = true;

			foreach ( Logic logic in m_logic_list )
			{
				bool val = logic.Evaluate();

				if ( first_logic )
				{
					result = val;
				}
				else
				{
					if ( m_and_values )
					{
						result = result && val;
					}
					else
					{
						result = result || val;
					}
				}
			}

			return result;
		}

		public void Write( XmlTextWriter writer )
		{
			writer.WriteStartElement("Expression");
			writer.WriteElementString("AndValues", m_and_values.ToString());
			foreach ( Logic logic in m_logic_list )
			{
				logic.Write( writer );
			}
			writer.WriteEndElement();
		}

		public void Read ( XmlTextReader reader, Thinker thinker )
		{
			bool done = false;

			while ( !done ) 
			{
				reader.Read();

				if ( reader.NodeType == XmlNodeType.EndElement &&
					reader.Name == "Expression" )
				{
					done =true;
				}
					// Process a start of element node.
				else if (reader.NodeType == XmlNodeType.Element) 
				{ 
					// Process a text node.
					if ( reader.Name == "Logic" ) 
					{
						Logic logic = new Logic();
						logic.Read( reader, thinker );
						m_logic_list.Add( logic );
					}
					else if ( reader.Name == "AndValues" )
					{
						while (reader.NodeType != XmlNodeType.Text) 
						{
							reader.Read();
						}
						m_and_values = bool.Parse(reader.Value);
					}
				}
			}// End while loop
		}
	}
}
