using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Drawing;


namespace GameEngine
{
	/// <summary>
	/// Summary description for Console.
	/// </summary>
	public class Console : IDisposable
	{

		#region Attributes
		private static bool          m_bVisible = false;
		private static bool          m_bOpening = false;
		private static bool          m_bClosing = false;
		private static ArrayList     m_Entries  = new ArrayList();
		private static SortedList    m_Commands = new SortedList();
		private static SortedList    m_Parameters = new SortedList();
		private static GraphicsFont  m_pFont = null;  // font for screen text rendering
		private static StringBuilder m_Entryline = new StringBuilder();
		private static Image         m_Image;
		private static float         m_Percent = 0.0f;
		private static float         m_MaxPercent = 0.50f;
		#endregion

		/// <summary>
		/// constructor uses the font to print text with as well as the background image
		/// </summary>
		public Console( GraphicsFont pFont, string sFilename)
		{
			m_pFont = pFont;
			m_Image = new Image( sFilename );

			Reset();

			AddCommand("SET", "Set a paramter to a value", new CommandFunction(Set));
			AddCommand("HELP", "Display command and Parameter help", new CommandFunction(Help));
			AddParameter("CONSOLESIZE", "The percentage of the screen covered by the console from 1.0 to 100.0",
				new CommandFunction(SetScreenSize));
		}

		public static void Reset()
		{
			m_Entries.Clear();

			AddLine("type 'Help' or 'help command' for command descriptions");

			m_Entryline = new StringBuilder(">");

		}

		/// <summary>
		/// Set the max console size between 0% and 100%
		/// </summary>
		/// <param name="fPercent"></param>
		public void SetMaxScreenSize( float fPercent )
		{
			if ( fPercent < 10.0f )  fPercent = 10.0f;
			if ( fPercent > 100.0f ) fPercent = 100.0f;
			m_MaxPercent = fPercent / 100.0f;
		}

		/// <summary>
		/// Set the max console size between 0% and 100%
		/// </summary>
		/// <param name="fPercent"></param>
		public void SetScreenSize( string sPercent )
		{
			float f;
			try 
			{
				f = float.Parse(sPercent);
			}
			catch
			{
				f = 50.0f;
			}
			SetMaxScreenSize( f );
		}

		/// <summary>
		/// Draw the console if it is visible
		/// </summary>
		public static void Render()
		{
			if ( m_bVisible )
			{
				bool fog_state = CGameEngine.Device3D.RenderState.FogEnable;

				CGameEngine.Device3D.RenderState.FogEnable = false;

				// determine how much of the console will be visible based on whether it is opening,
				// open, or closing
				if ( m_bOpening && m_Percent <= m_MaxPercent ) 
				{
					m_Percent += 0.05f;

				} 
				else if ( m_bClosing && m_Percent >= 0.0f) 
				{
					m_Percent -= 0.05f;
					if ( m_Percent <= 0.0f ) 
					{
						m_bClosing = false;
						m_bVisible = false;
					}
				} 

				// render the console background
				try
				{
					int line = (int)((m_Percent*CGameEngine.Device3D.Viewport.Height) - 5 - m_pFont.LineHeight);

					if ( line > 5 )
					{
						// draw the image to the device
						try
						{
							CustomVertex.TransformedTextured[] data = new CustomVertex.TransformedTextured[4];
							data[0].X =    CGameEngine.Device3D.Viewport.Width; 
							data[0].Y =    0.0f - (1.0f-m_Percent)*CGameEngine.Device3D.Viewport.Height;
							data[0].Z =    0.0f;
							data[0].Tu =   1.0f;
							data[0].Tv =   0.0f;
							data[1].X =  0.0f;   
							data[1].Y =    0.0f - (1.0f-m_Percent)*CGameEngine.Device3D.Viewport.Height;
							data[1].Z =    0.0f;
							data[1].Tu =   0.0f;
							data[1].Tv =   0.0f;
							data[2].X =    CGameEngine.Device3D.Viewport.Width; 
							data[2].Y =  CGameEngine.Device3D.Viewport.Height - (1.0f-m_Percent)*CGameEngine.Device3D.Viewport.Height;
							data[2].Z =    0.0f;
							data[2].Tu =   1.0f;
							data[2].Tv =   1.0f;
							data[3].X =  0.0f; 
							data[3].Y =  CGameEngine.Device3D.Viewport.Height - (1.0f-m_Percent)*CGameEngine.Device3D.Viewport.Height;
							data[3].Z =    0.0f;
							data[3].Tu =   0.0f;
							data[3].Tv =   1.0f;

							VertexBuffer vb = new VertexBuffer( typeof(CustomVertex.TransformedTextured), 4,
								CGameEngine.Device3D, Usage.WriteOnly, CustomVertex.TransformedTextured.Format,
								Pool.Default );

							vb.SetData(data, 0, 0);

							CGameEngine.Device3D.SetStreamSource( 0, vb, 0 );
							CGameEngine.Device3D.VertexFormat = CustomVertex.TransformedTextured.Format;
							CGameEngine.Device3D.RenderState.CullMode = Microsoft.DirectX.Direct3D.Cull.Clockwise;

							// Set the texture
							CGameEngine.Device3D.SetTexture(0, m_Image.GetTexture() );

							// Render the face
							CGameEngine.Device3D.DrawPrimitives( PrimitiveType.TriangleStrip, 0, 2 );
						}
						catch (DirectXException d3de)
						{
							Console.AddLine( "Unable to display console " );
							Console.AddLine( d3de.ErrorString );
						}
						catch ( Exception e )
						{
							Console.AddLine( "Unable to display console " );
							Console.AddLine( e.Message );
						}

						m_pFont.DrawText( 2,  line, Color.White, m_Entryline.ToString() );
						line -= (int)m_pFont.LineHeight;

						foreach ( String entry in m_Entries ) 
						{
							if ( line < 5 || line > 300 )
							{
//								Debug.WriteLine("line is " + line);
							}
							if ( line > 5 ) 
							{
								m_pFont.DrawText( 2,  line, Color.White, entry );
								line -= (int)m_pFont.LineHeight;
							}
						}
					}

				}
				catch (DirectXException d3de)
				{
					Debug.WriteLine("unable to render console");
					Debug.WriteLine(d3de.ErrorString);
				}
				catch ( Exception e )
				{
					Debug.WriteLine("unable to render console");
					Debug.WriteLine(e.Message);
				}

				CGameEngine.Device3D.RenderState.FogEnable = fog_state;
			}
		}

		/// <summary>
		/// add a line to the bottom of the console
		/// </summary>
		/// <param name="sNewLine"></param>
		public static void AddLine( string sNewLine )
		{
			m_Entries.Insert(0,sNewLine);
			if ( m_Entries.Count > 50 )
			{
				m_Entries.RemoveAt(50);
			}
			System.Diagnostics.Debug.WriteLine(sNewLine);
		}

		/// <summary>
		/// Add a new character to the input line (normally a typed character)
		/// </summary>
		/// <param name="sNewCharacter"></param>
		public static void AddCharacterToEntryLine( char sNewCharacter )
		{
			if ( m_bVisible ) m_Entryline.Append(sNewCharacter);
		}

		/// <summary>
		/// Respond to a cairrage return and process the entry line
		/// </summary>
		public static void ProcessEntry()
		{
			string sCommand = m_Entryline.ToString().Substring(1,m_Entryline.Length-1);
			AddLine(sCommand);
			m_Entryline.Remove(1,m_Entryline.Length-1);
			ParseCommand(sCommand);
		}

		/// <summary>
		/// Respond to a backspace and remove the last character on the entry line
		/// </summary>
		public static void Backspace()
		{
			if ( m_Entryline.Length > 1 ) 
			{
				m_Entryline.Remove(m_Entryline.Length-1,1);
			}
		}

		/// <summary>
		/// Open and close the console
		/// </summary>
		public static void ToggleState()
		{
			if ( m_bVisible ) 
			{
				m_bClosing = true;
				m_bOpening = false;
			} 
			else 
			{
				m_bOpening = true;
				m_bVisible = true;
			}
		}

		/// <summary>
		/// Get the console's visibility status
		/// </summary>
		/// <returns></returns>
		public static bool IsVisible
		{
				get 
		  {
			  return m_bVisible;
		  }
		}

		/// <summary>
		/// Break the entry into two parts, command and data and pass them onto the command processor
		/// </summary>
		/// <param name="sCommand"></param>
		private static void ParseCommand( string sCommand )
		{
			// remove any extra white space
			sCommand.Trim();

			// find the space between the command and the data (if any)
			int nSpace = sCommand.IndexOf(" ");

			// is there any data
			if ( nSpace > 0 ) 
			{
				string sCmd = sCommand.Substring(0,nSpace);
				string sData = sCommand.Remove(0,nSpace+1);
				ProcessCommand( sCmd, sData );
			} 
			else 
			{
				ProcessCommand( sCommand, null );
			}

		}

		private static void ProcessCommand( string sCmd, string sData )
		{
			int nIndex = m_Commands.IndexOfKey( sCmd );

			if ( nIndex < 0 ) // not found
			{
				AddLine("Unrecognized Command");
			}
			else
			{
				GameCommand Cmd = (GameCommand)m_Commands.GetByIndex(nIndex);
				Cmd.Execute( sData );
			}
		}

		/// <summary>
		/// Add a console command mapping
		/// </summary>
		/// <param name="sCmd"></param>
		/// <param name="sHelp"></param>
		/// <param name="Func"></param>
		public static void AddCommand( string sCmd, string sHelp, CommandFunction Func )
		{
			GameCommand Cmd = new GameCommand(sCmd, sHelp, Func);
			m_Commands.Add(sCmd, Cmd);
		}

		/// <summary>
		/// Unmap a command
		/// </summary>
		/// <param name="sCmd"></param>
		public static void RemoveCommand( string sCmd )
		{
			int nIndex = m_Commands.IndexOfKey(sCmd);
			if ( nIndex >= 0 ) 
			{
				m_Commands.RemoveAt(nIndex);
			}
		}
		/// <summary>
		/// Add a console parameter mapping
		/// </summary>
		/// <param name="sParam"></param>
		/// <param name="sHelp"></param>
		/// <param name="Func"></param>
		public static void AddParameter( string sParam, string sHelp, CommandFunction Func )
		{
			GameCommand Cmd = new GameCommand(sParam, sHelp, Func);
			m_Parameters.Add(sParam, Cmd);
		}

		/// <summary>
		/// Unmap a command
		/// </summary>
		/// <param name="sParam"></param>
		public static void RemoveParameter( string sParam )
		{
			int nIndex = m_Parameters.IndexOfKey(sParam);
			if ( nIndex >= 0 ) 
			{
				m_Parameters.RemoveAt(nIndex);
			}
		}

		private void Help( string sData )
		{
			StringBuilder sTemp = new StringBuilder();
			if ( sData == null )
			{
				AddLine("Valid Commands");
				foreach ( string sCmds in m_Commands.Keys ) 
				{
					sTemp.Append(sCmds);
					sTemp.Append(" ");
					if ( sTemp.Length > 40 )
					{
						AddLine(sTemp.ToString());
						sTemp.Remove(0,sTemp.Length-1);
					}
				}
				if ( sTemp.Length > 0 )
				{
					AddLine(sTemp.ToString());
				}
			}
			else
			{
				int nIndex = m_Commands.IndexOfKey( sData );

				if ( nIndex < 0 ) // not found
				{
					nIndex = m_Parameters.IndexOfKey( sData );

					if ( nIndex < 0 ) // not found
					{
						AddLine("Unrecognized Command");
					}
					else
					{
						GameCommand Cmd = (GameCommand)m_Parameters.GetByIndex(nIndex);
						string sHelpText = sData + " - " + Cmd.Help;
						AddLine(sHelpText);
					}
				}
				else
				{
					GameCommand Cmd = (GameCommand)m_Commands.GetByIndex(nIndex);
					string sHelpText = sData + " - " + Cmd.Help;
					AddLine(sHelpText);
				}
			}
			if ( sData == "SET" )
			{
				AddLine("Valid Parameters");
				foreach ( string sCmds in m_Parameters.Keys ) 
				{
					sTemp.Append(sCmds);
					sTemp.Append(" ");
					if ( sTemp.Length > 40 )
					{
						AddLine(sTemp.ToString());
						sTemp.Remove(0,sTemp.Length-1);
					}
				}
				if ( sTemp.Length > 0 )
				{
					AddLine(sTemp.ToString());
				}
			}
		}

		private void Set( string data )
		{
			StringBuilder sTemp = new StringBuilder();

			int nSpace = data.IndexOf(" ");

			if ( nSpace > 0 ) 
			{
				string sCmd = data.Substring(0,nSpace);
				string sData = data.Remove(0,nSpace+1);
				int nIndex = m_Parameters.IndexOfKey( sCmd );

				if ( nIndex < 0 ) // not found
				{
					AddLine("Unrecognized Parameter");
				}
				else
				{
					GameCommand Cmd = (GameCommand)m_Parameters.GetByIndex(nIndex);
					Cmd.Execute( sData );
				}
			}
		}
		/// <summary>
		/// Dispose of the surface when we are done with it to free up video card memory
		/// </summary>
		public void Dispose()
		{
			m_Image.Dispose();
		}

	}
}
