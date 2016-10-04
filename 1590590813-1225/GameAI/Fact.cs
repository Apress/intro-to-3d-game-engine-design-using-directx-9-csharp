using System;
using System.Xml;

namespace GameAI
{
	public enum Operator {
		Equals, 
		LessThan, 
		GreaterThan, 
		LessThanEquals,
		GreaterThanEquals,
		NotEqual,
		And,
		Or,
		True,
		False
	};
	/// <summary>
	/// Summary description for Fact
	/// </summary>
	public class Fact
	{
		#region Attributes
		private string m_Name;
		private float m_Value;

		private static float m_Epsilon = 0.001f;
		#endregion
		#region Properties
		public string Name { get { return m_Name; } }
		public float Value { get { return m_Value; } set { m_Value = value; } }
		public bool IsTrue { get { return (Math.Abs(m_Value) > m_Epsilon); } }

		public static float Epsilon { set { m_Epsilon = value; } }
		#endregion

		public Fact(string name)
		{
			m_Name = name;
		}

		public void Write( XmlTextWriter writer )
		{
			writer.WriteStartElement("Fact");
			writer.WriteElementString("name", m_Name);
			writer.WriteElementString("Value", XmlConvert.ToString(m_Value));
			writer.WriteEndElement();
		}
	}
}
