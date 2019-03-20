using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace CellReport.WinForms.Presenters.WinForms
{
	public partial class GestureControl : UserControl
	{
		private Point _ptFirst = new Point();
		private Point _ptSecond = new Point();
		private int _iArguments = 0;

		// One of the fields in GESTUREINFO structure is type of Int64 (8 bytes).
		// The relevant gesture information is stored in lower 4 bytes. This
		// bit mask is used to get 4 lower bytes from this argument.
		private const Int64 ULL_ARGUMENTS_BIT_MASK = 0x00000000FFFFFFFF;

		//-----------------------------------------------------------------------
		// Multitouch/Touch glue (from winuser.h file)
		// Since the managed layer between C# and WinAPI functions does not 
		// exist at the moment for multi-touch related functions this part of 
		// code is required to replicate definitions from winuser.h file.
		//-----------------------------------------------------------------------
		// Touch event window message constants [winuser.h]
		private const int WM_GESTURENOTIFY = 0x011A;
		private const int WM_GESTURE = 0x0119;

		private const int GC_ALLGESTURES = 0x00000001;

		// Gesture IDs 
		private const int GID_BEGIN = 1;
		private const int GID_END = 2;
		private const int GID_ZOOM = 3;
		private const int GID_PAN = 4;
		private const int GID_ROTATE = 5;
		private const int GID_TWOFINGERTAP = 6;
		private const int GID_PRESSANDTAP = 7;

		// Gesture flags - GESTUREINFO.dwFlags
		private const int GF_BEGIN = 0x00000001;
		private const int GF_INERTIA = 0x00000002;
		private const int GF_END = 0x00000004;

		//
		// Gesture configuration structure
		//   - Used in SetGestureConfig and GetGestureConfig
		//   - Note that any setting not included in either GESTURECONFIG.dwWant
		//     or GESTURECONFIG.dwBlock will use the parent window's preferences
		//     or system defaults.
		//
		// Touch API defined structures [winuser.h]
		[StructLayout(LayoutKind.Sequential)]
		private struct GESTURECONFIG
		{
			public int dwID;    // gesture ID
			public int dwWant;  // settings related to gesture ID that are to be
													// turned on
			public int dwBlock; // settings related to gesture ID that are to be
													// turned off
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct POINTS
		{
			public short x;
			public short y;
		}

		//
		// Gesture information structure
		//   - Pass the HGESTUREINFO received in the WM_GESTURE message lParam 
		//     into the GetGestureInfo function to retrieve this information.
		//   - If cbExtraArgs is non-zero, pass the HGESTUREINFO received in 
		//     the WM_GESTURE message lParam into the GetGestureExtraArgs 
		//     function to retrieve extended argument information.
		//
		[StructLayout(LayoutKind.Sequential)]
		public struct GESTUREINFO
		{
			public int cbSize;           // size, in bytes, of this structure
																	 // (including variable length Args 
																	 // field)
			public int dwFlags;          // see GF_* flags
			public int dwID;             // gesture ID, see GID_* defines
			public IntPtr hwndTarget;    // handle to window targeted by this 
																	 // gesture
			[MarshalAs(UnmanagedType.Struct)]
			internal POINTS ptsLocation; // current location of this gesture
			public int dwInstanceID;     // internally used
			public int dwSequenceID;     // internally used
			public Int64 ullArguments;   // arguments for gestures whose 
																	 // arguments fit in 8 BYTES
			public int cbExtraArgs;      // size, in bytes, of extra arguments, 
																	 // if any, that accompany this gesture

			/// <summary>
			/// Gets a value indicating whether the <see cref="GestureFlags"/> Begin is set.
			/// </summary>
			public bool Begin
			{
				get { return ((GestureFlags)dwFlags & GestureFlags.Begin) == GestureFlags.Begin; }
			}

			/// <summary>
			/// Gets a value indicating whether the <see cref="GestureFlags"/> End is set.
			/// </summary>
			public bool End
			{
				get { return ((GestureFlags)dwFlags & GestureFlags.End) == GestureFlags.End; }
			}

			public bool Inertia
			{
				get { return ((GestureFlags)dwFlags & GestureFlags.Inertia) == GestureFlags.Inertia; }
			}
		}

		/// </summary>
		[Flags]
		public enum GestureFlags
		{
			/// <summary>
			/// GF_BEGIN
			/// </summary>
			Begin = 0x1,
			/// <summary>
			/// GF_INERTIA
			/// </summary>
			Inertia = 0x2,
			/// <summary>
			/// GF_END
			/// </summary>
			End = 0x4
		}

		#region GestureEventArgs Class

		/// <summary>
		/// Base class for all the gesture based events
		/// </summary>
		public abstract class GestureEventArgs : EventArgs
		{
			internal GestureEventArgs(GESTUREINFO info)
			{
				Location = new Point(info.ptsLocation.x, info.ptsLocation.y);
				Info = info;
				Handled = true;
			}

			#region Helper Methods

			protected static int LoDWord(IntPtr lParam)
			{
				return LoDWord(lParam.ToInt64());
			}

			protected static int HiDWord(IntPtr lParam)
			{
				return HiDWord(lParam.ToInt64());
			}

			protected static int LoDWord(long l)
			{
				return (int)(l & 0xFFFFFFFF);
			}

			protected static int HiDWord(long l)
			{
				return (int)((l >> 32) & 0xFFFFFFFF);
			}

			protected static short LoWord(int i)
			{
				return (short)(i & 0xFFFF);
			}

			protected static short HiWord(int i)
			{
				return (short)((i >> 16) & 0xFFFF);
			}

			#endregion

			#region Properties

			protected GESTUREINFO Info { get; set; }

			/// <summary>
			/// Gets the location of the gesture in Screen (not client) coordinates.
			/// </summary>
			public Point Location { get; private set; }

			/// <summary>
			/// Gets a value indicating whether this <see cref="GestureEventArgs"/> is beginning.
			/// </summary>
			public bool Begin
			{
				get { return Info.Begin; }
			}

			/// <summary>
			/// Gets a value indicating whether this <see cref="GestureEventArgs"/> is ending
			/// </summary>
			public bool End
			{
				get { return Info.End; }
			}

			/// <summary>
			/// Gets or sets a value indicating whether the window message was handled. Set this to false if you don't handle the message.
			/// </summary>
			public bool Handled { get; set; }

			#endregion
		}

		#endregion

		#region PanEventArgs Class

		/// <summary>
		/// Event for the Pan gesture
		/// </summary>
		public class PanEventArgs : GestureEventArgs
		{
			internal PanEventArgs(GESTUREINFO info, Point panPoint, Point lastPanPoint)
					: base(info)
			{
				int hiword = HiDWord(info.ullArguments);
				InertiaVector = new Point(LoWord(hiword), HiWord(hiword));
				PanOffset = new Point(panPoint.X - lastPanPoint.X, panPoint.Y - lastPanPoint.Y);
			}

			/// <summary>
			/// Gets a value indicating whether this <see cref="GestureEventArgs"/> has triggered inertia.
			/// </summary>
			public bool Inertia
			{
				get { return Info.Inertia; }
			}

			public Point InertiaVector { get; private set; }

			/// <summary>
			/// Gets the pan offset since the last pan message.
			/// </summary>
			public Point PanOffset { get; private set; }
		}

		#endregion

		#region ZoomEventArgs Class

		/// <summary>
		/// Event for the Zoom gesture
		/// </summary>
		public class ZoomEventArgs : GestureEventArgs
		{
			internal ZoomEventArgs(GESTUREINFO info, double percent)
					: base(info)
			{
				Distance = info.ullArguments;
				PercentChange = percent;
			}

			/// <summary>
			/// Gets the distance between the two points as they are being zoomed.
			/// </summary>
			public long Distance { get; private set; }

			/// <summary>
			/// Gets the percent changed since the last zoom message
			/// </summary>
			public double PercentChange { get; private set; }
		}

		#endregion

		#region PressAndTapEventArgs Class

		/// <summary>
		/// Event for the Press and Tap gesture
		/// </summary>
		public class PressAndTapEventArgs : GestureEventArgs
		{
			internal PressAndTapEventArgs(GESTUREINFO info)
					: base(info)
			{
				int pointsStruct = LoDWord(info.ullArguments);
				Distance = new Point(LoWord(pointsStruct), HiWord(pointsStruct));
			}

			/// <summary>
			/// Gets the distance between the two points.
			/// </summary>
			public Point Distance { get; private set; }
		}

		#endregion

		#region RotateEventArgs Class

		/// <summary>
		/// Event for the Rotate gesture
		/// </summary>
		public class RotateEventArgs : GestureEventArgs
		{
			internal RotateEventArgs(GESTUREINFO info, double lastRotation)
					: base(info)
			{
				int loword = LoDWord(info.ullArguments);
				TotalAngle = RotateAngleFromArgument(loword);
				Angle = TotalAngle - lastRotation;

				string msg = string.Format("Total:{0} Angle:{1} Last:{2}", TotalAngle, Angle, lastRotation);
				Debug.WriteLine(msg);
			}

			/// <summary>
			/// Gesture argument helper that converts an argument to a rotation angle.
			/// </summary>
			/// <param name="arg">The argument to convert. Should be an unsigned 16-bit value.</param>
			/// <returns></returns>
			private static double RotateAngleFromArgument(int arg)
			{
				return ((arg / 65535.0) * 4.0 * Math.PI) - 2.0 * Math.PI;
			}

			/// <summary>
			/// Gets the angle of rotation in Radians since the beginning of the gesture
			/// </summary>
			public double TotalAngle { get; private set; }

			/// <summary>
			/// Gets the angle of rotation in Degrees since the beginning of the gesture
			/// </summary>
			public double TotalDegrees
			{
				get { return RadiandsToDegrees(TotalAngle); }
			}

			/// <summary>
			/// Gets the angle of rotation in Radians since the last rotation message
			/// </summary>
			public double Angle { get; private set; }

			/// <summary>
			/// Gets the angle of rotation in Degrees since the last rotation message
			/// </summary>
			public double Degrees
			{
				get { return RadiandsToDegrees(Angle); }
			}

			private double RadiandsToDegrees(double radians)
			{
				return radians * 180.0 / Math.PI;
			}
		}

		#endregion

		#region TwoFingerTapEventArgs Class

		/// <summary>
		/// Base class for the Two Finger Tap gesture
		/// </summary>
		public class TwoFingerTapEventArgs : GestureEventArgs
		{
			internal TwoFingerTapEventArgs(GESTUREINFO info)
					: base(info)
			{
				Distance = info.ullArguments;
			}

			/// <summary>
			/// Gets the distance between the two points.
			/// </summary>
			public long Distance { get; private set; }
		}

		#endregion
		// Currently touch/multitouch access is done through unmanaged code
		// We must p/invoke into user32 [winuser.h]
		[DllImport("user32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetGestureConfig(IntPtr hWnd, int dwReserved, int cIDs, ref GESTURECONFIG pGestureConfig, int cbSize);

		[DllImport("user32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetGestureInfo(IntPtr hGestureInfo, ref GESTUREINFO pGestureInfo);

		// size of GESTURECONFIG structure
		private int _gestureConfigSize;
		// size of GESTUREINFO structure
		private int _gestureInfoSize;

		[SecurityPermission(SecurityAction.Demand)]
		private void SetupStructSizes()
		{
			// Both GetGestureCommandInfo and GetTouchInputInfo need to be
			// passed the size of the structure they will be filling
			// we get the sizes upfront so they can be used later.
			_gestureConfigSize = Marshal.SizeOf(new GESTURECONFIG());
			_gestureInfoSize = Marshal.SizeOf(new GESTUREINFO());
		}


		//-------------------------------------------------------------
		// Since there is no managed layer at the moment that supports
		// event handlers for WM_GESTURENOTIFY and WM_GESTURE
		// messages we have to override WndProc function
		// 
		// in 
		//   m - Message object
		//-------------------------------------------------------------
		[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
		protected override void WndProc(ref Message m)
		{
			bool handled;
			handled = false;

			switch (m.Msg)
			{
				case WM_GESTURENOTIFY:
					{
						// This is the right place to define the list of gestures
						// that this application will support. By populating 
						// GESTURECONFIG structure and calling SetGestureConfig 
						// function. We can choose gestures that we want to 
						// handle in our application. In this app we decide to 
						// handle all gestures.
						GESTURECONFIG gc = new GESTURECONFIG();
						gc.dwID = 0;                // gesture ID
						gc.dwWant = GC_ALLGESTURES; // settings related to gesture
																				// ID that are to be turned on
						gc.dwBlock = 0; // settings related to gesture ID that are
														// to be     

						// We must p/invoke into user32 [winuser.h]
						bool bResult = SetGestureConfig(
								Handle, // window for which configuration is specified
								0,      // reserved, must be 0
								1,      // count of GESTURECONFIG structures
								ref gc, // array of GESTURECONFIG structures, dwIDs 
												// will be processed in the order specified 
												// and repeated occurances will overwrite 
												// previous ones
								_gestureConfigSize // sizeof(GESTURECONFIG)
						);

						if (!bResult)
						{
							throw new Exception("Error in execution of SetGestureConfig");
						}
					}
					handled = true;
					break;

				case WM_GESTURE:
					// The gesture processing code is implemented in 
					// the DecodeGesture method
					handled = DecodeGesture(ref m);
					break;

				default:
					handled = false;
					break;
			}

			// Filter message back up to parents.
			if (!handled)
				base.WndProc(ref m);

			if (handled)
			{
				// Acknowledge event if handled.
				try
				{
					m.Result = new System.IntPtr(1);
				}
				catch (Exception excep)
				{
					Debug.Print("Could not allocate result ptr");
					Debug.Print(excep.ToString());
				}
			}
		}

		// Taken from GCI_ROTATE_ANGLE_FROM_ARGUMENT.
		// Converts from "binary radians" to traditional radians.
		static protected double ArgToRadians(Int64 arg)
		{
			return ((((double)(arg) / 65535.0) * 4.0 * 3.14159265) - 2.0 * 3.14159265);
		}

		private double _lastRotation;
		// Handler of gestures
		//in:
		//  m - Message object
		private bool DecodeGesture(ref Message m)
		{
			GESTUREINFO gi;

			try
			{
				gi = new GESTUREINFO();
			}
			catch (Exception excep)
			{
				Debug.Print("Could not allocate resources to decode gesture");
				Debug.Print(excep.ToString());

				return false;
			}

			gi.cbSize = _gestureInfoSize;

			// Load the gesture information.
			// We must p/invoke into user32 [winuser.h]
			if (!GetGestureInfo(m.LParam, ref gi))
			{
				return false;
			}

			switch (gi.dwID)
			{
				case GID_BEGIN:
				case GID_END:
					break;

				case GID_ZOOM:
					switch (gi.dwFlags)
					{
						case GF_BEGIN:
							_iArguments = (int)(gi.ullArguments & ULL_ARGUMENTS_BIT_MASK);
							_ptFirst.X = gi.ptsLocation.x;
							_ptFirst.Y = gi.ptsLocation.y;
							_ptFirst = PointToClient(_ptFirst);

							OnZoom(new ZoomEventArgs(gi, 0));
							break;

						default:
							// We read here the second point of the gesture. This
							// is middle point between fingers in this new 
							// position.
							_ptSecond.X = gi.ptsLocation.x;
							_ptSecond.Y = gi.ptsLocation.y;
							_ptSecond = PointToClient(_ptSecond);
							{
								// We have to calculate zoom center point 
								Point ptZoomCenter = new Point((_ptFirst.X + _ptSecond.X) / 2,
																						(_ptFirst.Y + _ptSecond.Y) / 2);

								// The zoom factor is the ratio of the new
								// and the old distance. The new distance 
								// between two fingers is stored in 
								// gi.ullArguments (lower 4 bytes) and the old 
								// distance is stored in _iArguments.
								double k = (double)(gi.ullArguments & ULL_ARGUMENTS_BIT_MASK) /
														(double)(_iArguments);
								// Now we process zooming in/out of the object
								//OnZoom(k, ptZoomCenter.X, ptZoomCenter.Y);
								OnZoom(new ZoomEventArgs(gi, k));

								Invalidate();
							}

							// Now we have to store new information as a starting
							// information for the next step in this gesture.
							_ptFirst = _ptSecond;
							_iArguments = (int)(gi.ullArguments & ULL_ARGUMENTS_BIT_MASK);
							break;
						case GF_END:
							OnZoom(new ZoomEventArgs(gi, 0));
							break;
					}
					break;

				case GID_PAN:
					switch (gi.dwFlags)
					{
						case GF_BEGIN:
							_ptFirst.X = gi.ptsLocation.x;
							_ptFirst.Y = gi.ptsLocation.y;
							//_ptFirst = PointToClient(_ptFirst);
							OnPan(new PanEventArgs(gi, _ptFirst, _ptFirst));
							break;

						default:
							// We read the second point of this gesture. It is a
							// middle point between fingers in this new position
							_ptSecond.X = gi.ptsLocation.x;
							_ptSecond.Y = gi.ptsLocation.y;
							//_ptSecond = PointToClient(_ptSecond);

							// We apply move operation of the object
							//OnPan(_ptSecond.X - _ptFirst.X, _ptSecond.Y - _ptFirst.Y);
							OnPan(new PanEventArgs(gi, _ptSecond, _ptFirst));

							// We have to copy second point into first one to
							// prepare for the next step of this gesture.
							_ptFirst = _ptSecond;
							break;
					}
					break;

				case GID_ROTATE:
					switch (gi.dwFlags)
					{
						case GF_BEGIN:
							_iArguments = 0;
							break;

						default:
							_ptFirst.X = gi.ptsLocation.x;
							_ptFirst.Y = gi.ptsLocation.y;
							_ptFirst = PointToClient(_ptFirst);

							if (gi.Begin)
								_lastRotation = 0;
							// Gesture handler returns cumulative rotation angle. However we
							// have to pass the delta angle to our function responsible 
							// to process the rotation gesture.
							var angle = ArgToRadians(gi.ullArguments & ULL_ARGUMENTS_BIT_MASK) - ArgToRadians(_iArguments);
							//OnRotate(angle, _ptFirst.X, _ptFirst.Y);
							var args = new RotateEventArgs(gi, _lastRotation);
							OnRotate(args);
							if (!gi.Begin)
							{
								// First rotation is the angle the fingers are at, so don't use it
								_lastRotation = args.TotalAngle;
							}

							_iArguments = (int)(gi.ullArguments & ULL_ARGUMENTS_BIT_MASK);
							break;
					}
					break;

				case GID_TWOFINGERTAP:
					OnTwoFingerTap(new TwoFingerTapEventArgs(gi));
					break;
				case GID_PRESSANDTAP:
					if (gi.dwFlags == GF_BEGIN)
					{
						OnPressAndTap(new PressAndTapEventArgs(gi));
					}
					break;
			}

			return true;
		}

		public GestureControl()
		{
			InitializeComponent();
			SetupStructSizes();
		}

		protected virtual void OnRotate(RotateEventArgs e)
		{
			//Debug.WriteLine($"OnRotate: angle: {e.Angle} begin: {e.Begin}");
		}

		protected virtual void OnPan(PanEventArgs e)
		{
			//Debug.WriteLine($"OnPan: deltaX: {e.PanOffset.X} deltaY: {e.PanOffset.Y}");
		}

		protected virtual void OnZoom(ZoomEventArgs e)
		{
			//Debug.WriteLine($"OnZoom: begin: {e.Begin} distance: {e.Distance} percent: {e.PercentChange}");
		}

		protected virtual void OnPressAndTap(PressAndTapEventArgs e)
		{
		}

		protected virtual void OnTwoFingerTap(TwoFingerTapEventArgs e)
		{
			//Debug.WriteLine("====== tap two ====");
		}
	}
}
