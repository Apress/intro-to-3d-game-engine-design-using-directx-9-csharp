using System;
using System.Xml;
using System.Diagnostics;

namespace GameAI
{
	/// <summary>
	/// Summary description for Logic.
	/// </summary>
	public class Logic
	{
		#region Attributes
		private Fact m_first;
		private Fact m_second;
		private Operator m_operator;
		#endregion

		#region Properties
		public Fact FirstFact { get { return m_first; } set { m_first = value; } }
		public Fact SecondFact { get { return m_second; } set { m_second = value; } }
		public Operator Operation { get { return m_operator; } set { m_operator = value; } }
		#endregion

		public Logic()
		{
			m_first = null;
			m_second = null;
			m_operator = Operator.Equals;
		}

		public Logic(Fact first, Fact second, Operator op)
		{
			m_first = first;
			m_second = second;
			m_operator = op;
		}

		public bool Evaluate()
		{
			bool result = false;

			if ( m_first != null )
			{
				switch ( m_operator )
				{
					case Operator.And:
						if ( m_second != null )
						{
							result = m_first.IsTrue && m_second.IsTrue;
						}
						else
						{
							Debug.WriteLine("second fact missing in Logic");
						}
						break;
					case Operator.Equals:
						if ( m_second != null )
						{
							result = m_first.Value == m_second.Value;
						}
						else
						{
							Debug.WriteLine("second fact missing in Logic");
						}
						break;
					case Operator.GreaterThan:
						if ( m_second != null )
						{
							result = m_first.Value > m_second.Value;
						}
						else
						{
							Debug.WriteLine("second fact missing in Logic");
						}
						break;
					case Operator.GreaterThanEquals:
						if ( m_second != null )
						{
							result = m_first.Value >= m_second.Value;
						}
						else
						{
							Debug.WriteLine("second fact missing in Logic");
						}
						break;
					case Operator.LessThan:
						if ( m_second != null )
						{
							result = m_first.Value < m_second.Value;
						}
						else
						{
							Debug.WriteLine("second fact missing in Logic");
						}
						break;
					case Operator.LessThanEquals:
						if ( m_second != null )
						{
							result = m_first.Value <= m_second.Value;
						}
						else
						{
							Debug.WriteLine("second fact missing in Logic");
						}
						break;
					case Operator.NotEqual:
						if ( m_second != null )
						{
							result = m_first.Value != m_second.Value;
						}
						else
						{
							Debug.WriteLine("second fact missing in Logic");
						}
						break;
					case Operator.Or:
						if ( m_second != null )
						{
							result = m_first.IsTrue || m_second.IsTrue;
						}
						else
						{
							Debug.WriteLine("second fact missing in Logic");
						}
						break;
					case Operator.True:
						result = m_first.IsTrue;
						break;
					case Operator.False:
						result = !m_first.IsTrue;
						break;
				}
			}
			else
			{
				Debug.WriteLine("first fact missing in Logic");
			}

			return result;
		}

		public void Write( XmlTextWriter writer )
		{
			writer.WriteStartElement("Logic");
			writer.WriteElementString("Fact1", m_first.Name);
			writer.WriteElementString("Operator", m_operator.ToString());
			writer.WriteElementString("Fact2", m_second.Name);
			writer.WriteEndElement();
		}

		public void Read ( XmlTextReader reader, Thinker thinker )
		{
			bool done = false;

			while ( !done ) 
			{
				reader.Read();

				if ( reader.NodeType == XmlNodeType.EndElement &&
					reader.Name == "Logic" )
				{
					done =true;
				}
					// Process a start of element node.
				else if (reader.NodeType == XmlNodeType.Element) 
				{ 
					// Process a text node.
					if ( reader.Name == "Fact1" ) 
					{
						while (reader.NodeType != XmlNodeType.Text) 
						{
							reader.Read();
						}
						m_first = thinker.GetFact(reader.Value);
					}
					if ( reader.Name == "Fact2" ) 
					{
						while (reader.NodeType != XmlNodeType.Text) 
						{
							reader.Read();
						}
						m_second = thinker.GetFact(reader.Value);
					}
					if ( reader.Name == "Operator" ) 
					{
						while (reader.NodeType != XmlNodeType.Text) 
						{
							reader.Read();
						}
						switch ( reader.Value )
						{
							case "And":
								m_operator = Operator.And;
								break;
							case "Equals":
								m_operator = Operator.Equals;
								break;
							case "GreaterThan":
								m_operator = Operator.GreaterThan;
								break;
							case "GreaterThanEquals":
								m_operator = Operator.GreaterThanEquals;
								break;
							case "LessThan":
								m_operator = Operator.LessThan;
								break;
							case "LessThanEquals":
								m_operator = Operator.LessThanEquals;
								break;
							case "NotEqual":
								m_operator = Operator.NotEqual;
								break;
							case "Or":
								m_operator = Operator.Or;
								break;
							case "True":
								m_operator = Operator.True;
								break;
							case "False":
								m_operator = Operator.False;
								break;
						}
					}
				}
			}// End while loop
		}
	}
}
