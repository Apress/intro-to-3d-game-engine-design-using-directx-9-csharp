//-----------------------------------------------------------------------------
// File: DXUtil.cs
//
// Desc: Shortcut macros and functions for using DX objects
//
// Copyright (c) 2001-2002 Microsoft Corporation. All rights reserved
//-----------------------------------------------------------------------------
using System;
using System.IO;
using System.Runtime.InteropServices;
public enum TIMER
{
	RESET, 
	START, 
	STOP, 
	ADVANCE,
	GETABSOLUTETIME, 
	GETAPPTIME, 
	GETELAPSEDTIME 
};

public class DXUtil
{
	#region Timer Internal Stuff
	[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
	[DllImport("kernel32")]
	private static extern bool QueryPerformanceFrequency(ref long PerformanceFrequency);
	[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
	[DllImport("kernel32")]
	private static extern bool QueryPerformanceCounter(ref long PerformanceCount);
	[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
	[DllImport("winmm.dll")]
	public static extern int timeGetTime();
	private static bool m_bTimerInitialized = false;
	private static bool m_bUsingQPF         = false;
	private static bool m_bTimerStopped     = true;
	private static long m_llQPFTicksPerSec  = 0;
	private static long m_llStopTime        = 0;
	private static long m_llLastElapsedTime = 0;
	private static long m_llBaseTime        = 0;
	private static double m_fLastElapsedTime  = 0.0;
	private static double m_fBaseTime         = 0.0;
	private static double m_fStopTime         = 0.0;
	#endregion

	// Constants for SDK Path registry keys
	private const string g_sSDKPath = "Software\\Microsoft\\DirectX SDK";
	private const string g_sSDKKey = "DX9SDK Samples Path";

	private DXUtil() { /* Private Constructor */ }



	//-----------------------------------------------------------------------------
	// Name: DXUtil.GetDXSDKMediaPath()
	// Desc: Returns the DirectX SDK media path
	//-----------------------------------------------------------------------------
	public static string GetDXSDKMediaPath()
	{
		Microsoft.Win32.RegistryKey rKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(g_sSDKPath);
		string sReg = null;
		if (rKey != null)
		{
			sReg = (string)rKey.GetValue(g_sSDKKey);
			rKey.Close();
		}
		if (sReg != null)
			sReg += @"\Media\";
		else 
			return null;

		return sReg;
	}



	//-----------------------------------------------------------------------------
	// Name: DXUtil.Timer()
	// Desc: Performs timer opertations. Use the following commands:
	//          TIMER.RESET           - to reset the timer
	//          TIMER.START           - to start the timer
	//          TIMER.STOP            - to stop (or pause) the timer
	//          TIMER.ADVANCE         - to advance the timer by 0.001 seconds
	//          TIMER.GETABSOLUTETIME - to get the absolute system time
	//          TIMER.GETAPPTIME      - to get the current time
	//          TIMER.GETELAPSEDTIME  - to get the time that elapsed between 
	//                                  TIMER_GETELAPSEDTIME calls
	//-----------------------------------------------------------------------------
	public static float Timer(TIMER command)
	{
		if( !m_bTimerInitialized )
		{
			m_bTimerInitialized = true;

			// Use QueryPerformanceFrequency() to get frequency of timer.  If QPF is
			// not supported, we will timeGetTime() which returns milliseconds.
			long qwTicksPerSec = 0;
			m_bUsingQPF = QueryPerformanceFrequency( ref qwTicksPerSec );
			if( m_bUsingQPF )
				m_llQPFTicksPerSec = qwTicksPerSec;  // in msec
		}
		if( m_bUsingQPF )
		{
			double fTime;
			double fElapsedTime;
			long qwTime = 0;
		    
			// Get either the current time or the stop time, depending
			// on whether we're stopped and what command was sent
			if( m_llStopTime != 0 && command != TIMER.START && command != TIMER.GETABSOLUTETIME)
				qwTime = m_llStopTime;
			else
				QueryPerformanceCounter( ref qwTime );

			// Return the elapsed time
			if( command == TIMER.GETELAPSEDTIME )
			{
				fElapsedTime = (double) ( qwTime - m_llLastElapsedTime ) / (double) m_llQPFTicksPerSec;
				m_llLastElapsedTime = qwTime;
				return (float)fElapsedTime;
			}
		
			// Return the current time
			if( command == TIMER.GETAPPTIME )
			{
				double fAppTime = (double) ( qwTime - m_llBaseTime ) / (double) m_llQPFTicksPerSec;
				return (float)fAppTime;
			}
		
			// Reset the timer
			if( command == TIMER.RESET )
			{
				m_llBaseTime        = qwTime;
				m_llLastElapsedTime = qwTime;
				m_llStopTime        = 0;
				m_bTimerStopped     = false;
				return 0.0f;
			}
		
			// Start the timer
			if( command == TIMER.START )
			{
				if( m_bTimerStopped )
					m_llBaseTime += qwTime - m_llStopTime;
				m_llStopTime = 0;
				m_llLastElapsedTime = qwTime;
				m_bTimerStopped = false;
				return 0.0f;
			}
		
			// Stop the timer
			if( command == TIMER.STOP )
			{
				m_llStopTime = qwTime;
				m_llLastElapsedTime = qwTime;
				m_bTimerStopped = true;
				return 0.0f;
			}
		
			// Advance the timer by millisecond
			if( command == TIMER.ADVANCE )
			{
				m_llStopTime += m_llQPFTicksPerSec/1000;
				return 0.0f;
			}

			if( command == TIMER.GETABSOLUTETIME )
			{
				fTime = qwTime / (double) m_llQPFTicksPerSec;
				return (float)fTime;
			}

			return -1.0f; // Invalid command specified
		}
		else
		{
			// Get the time using timeGetTime()
			double fTime;
			double fElapsedTime;
		    
			// Get either the current time or the stop time, depending
			// on whether we're stopped and what command was sent
			if( m_fStopTime != 0.0 && command != TIMER.START && command != TIMER.GETABSOLUTETIME)
				fTime = m_fStopTime;
			else
				fTime = timeGetTime();
		
			// Return the elapsed time
			if( command == TIMER.GETELAPSEDTIME )
			{   
				fElapsedTime = (double) (fTime - m_fLastElapsedTime);
				m_fLastElapsedTime = fTime;
				return (float) fElapsedTime;
			}
		
			// Return the current time
			if( command == TIMER.GETAPPTIME )
			{
				return (float) (fTime - m_fBaseTime);
			}
		
			// Reset the timer
			if( command == TIMER.RESET )
			{
				m_fBaseTime         = fTime;
				m_fLastElapsedTime  = fTime;
				m_fStopTime         = 0;
				m_bTimerStopped     = false;
				return 0.0f;
			}
		
			// Start the timer
			if( command == TIMER.START )
			{
				if( m_bTimerStopped )
					m_fBaseTime += fTime - m_fStopTime;
				m_fStopTime = 0.0f;
				m_fLastElapsedTime  = fTime;
				m_bTimerStopped = false;
				return 0.0f;
			}
		
			// Stop the timer
			if( command == TIMER.STOP )
			{
				m_fStopTime = fTime;
				m_fLastElapsedTime  = fTime;
				m_bTimerStopped = true;
				return 0.0f;
			}
		
			// Advance the timer by 1/10th second
			if( command == TIMER.ADVANCE )
			{
				m_fStopTime += 0.1f;
				return 0.0f;
			}

			if( command == TIMER.GETABSOLUTETIME )
			{
				return (float) fTime;
			}

			return -1.0f; // Invalid command specified
		}
	}



	//-----------------------------------------------------------------------------
	// Name: DXUtil.FindMediaFile()
	// Desc: Returns a valid path to a DXSDK media file
	//-----------------------------------------------------------------------------
	public static string FindMediaFile( string sPath, string sFilename )
	{
		// First try to load the file in the full path
		if (sPath != null)
		{
			if (File.Exists(AppendDirSep(sPath) + sFilename))
				return AppendDirSep(sPath) + sFilename;
		}

		// if not try to find the filename in the current folder.
		if (File.Exists(sFilename))
			return AppendDirSep(Directory.GetCurrentDirectory()) + sFilename; 

		// last, check if the file exists in the media directory
		if (File.Exists(AppendDirSep(GetDXSDKMediaPath()) + sFilename))
			return AppendDirSep(GetDXSDKMediaPath()) + sFilename;

		throw new FileNotFoundException("Could not find this file.", sFilename);
	}



	//-----------------------------------------------------------------------------
	// Name: DXUtil.AppendDirSep()
	// Desc: Returns a valid string with a directory separator at the end.
	//-----------------------------------------------------------------------------
	private static string AppendDirSep(string sFile)
	{
		if (!sFile.EndsWith(@"\"))
			return sFile + @"\";

		return sFile;
	}
}
