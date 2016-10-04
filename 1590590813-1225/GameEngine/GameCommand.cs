using System;
using System.Collections;

namespace GameEngine
{
	/// <summary>
	/// Summary description for GameCommand.
	/// </summary>
	public delegate void CommandFunction( string sData );

	public class GameCommand 
	{

		private string          m_sCommand = null;
		private string          m_sHelp    = null;
		private CommandFunction m_Function = null;

		public string Command { get { return m_sCommand; } }
		public string Help { get { return m_sHelp; } }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sCmd"></param>
		/// <param name="sHelp"></param>
		/// <param name="pFunc"></param>
		public GameCommand(string sCmd, string sHelp, CommandFunction pFunc )
		{
			m_sCommand = sCmd;
			m_sHelp = sHelp;
			m_Function = pFunc;
		}

		/// <summary>
		/// Execute the attached delegate function
		/// </summary>
		/// <param name="sData"></param>
		public void Execute( string sData )
		{
			if ( m_Function != null )
			{
				m_Function(sData);
			}
		}

	}
}
