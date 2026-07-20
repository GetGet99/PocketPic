#if HAS_UNO

#nullable disable

using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Uno.WinUI.Runtime.Skia.X11;

namespace PocketPic;

static class LinuxClipboard
{
	static readonly LinuxClipboardHelper _helper = new();

	public static void SetContent(DataPackage content) => _helper.SetContent(content);
	public static void Clear() => _helper.Clear();
	public static void Flush() => _helper.Flush();
}

sealed class LinuxClipboardHelper : IDisposable
{
	static readonly Dictionary<string, Encoding> TextFormats = new()
	{
		{ "UTF8_STRING", Encoding.UTF8 },
		{ "text/plain;charset=utf-8", Encoding.UTF8 },
		{ "UTF16_STRING", Encoding.Unicode },
		{ "text/plain;charset=utf-16", Encoding.Unicode },
		{ "XA_STRING", Encoding.ASCII },
		{ "OEMTEXT", Encoding.ASCII },
	};

	const int PropertyChangeMask = 1 << 22;

	static readonly IntPtr CurrentTime = IntPtr.Zero;
	static readonly IntPtr None = IntPtr.Zero;

	readonly IntPtr _display;
	readonly IntPtr _window;
	readonly object _xLock = new();
	readonly Dictionary<string, IntPtr> _atomCache = new();
	readonly Thread _eventThread;

	IntPtr _aCLIPBOARD;
	IntPtr _aTARGETS;
	IntPtr _aTIMESTAMP;
	IntPtr _aMULTIPLE;
	IntPtr _aINTEGER;
	IntPtr _aATOM;
	IntPtr _aATOM_PAIR;

	volatile bool _disposed;
	volatile bool _isOwner;
	IntPtr _timestamp;
	DataPackageView? _data;

	public LinuxClipboardHelper()
	{
		_display = XLib.XOpenDisplay(IntPtr.Zero);
		if (_display == IntPtr.Zero)
			throw new InvalidOperationException("Cannot connect to X11 display");

		var screen = XLib.XDefaultScreen(_display);
		_window = XLib.XCreateSimpleWindow(
			_display, XLib.XRootWindow(_display, screen),
			0, 0, 1, 1, 0, IntPtr.Zero, IntPtr.Zero);

		_aCLIPBOARD = GetAtom("CLIPBOARD");
		_aTARGETS = GetAtom("TARGETS");
		_aTIMESTAMP = GetAtom("TIMESTAMP");
		_aMULTIPLE = GetAtom("MULTIPLE");
		_aINTEGER = GetAtom("INTEGER");
		_aATOM = GetAtom("ATOM");
		_aATOM_PAIR = GetAtom("ATOM_PAIR");

		XLib.XFlush(_display);

		_eventThread = new Thread(EventLoop)
		{
			Name = "X11 Clipboard",
			IsBackground = true
		};
		_eventThread.Start();
	}

	IntPtr GetAtom(string name)
	{
		if (_atomCache.TryGetValue(name, out var atom))
			return atom;
		atom = XLib.XInternAtom(_display, name, false);
		_atomCache[name] = atom;
		return atom;
	}

	public void SetContent(DataPackage content)
	{
		lock (_xLock)
		{
			_timestamp = GetTimestamp();
			XLib.XSetSelectionOwner(_display, _aCLIPBOARD, _window, _timestamp);
			XLib.XFlush(_display);

			if (XLib.XGetSelectionOwner(_display, _aCLIPBOARD) == _window)
			{
				_data = content.GetView();
				_isOwner = true;
			}
			else
			{
				_isOwner = false;
			}
		}
	}

	public void Clear() => SetContent(new DataPackage());

	public void Flush() { }

	void EventLoop()
	{
		while (!_disposed)
		{
			int pending;
			lock (_xLock) { pending = XLib.XPending(_display); }

			if (pending > 0)
			{
				lock (_xLock)
				{
					while (!_disposed && XLib.XPending(_display) > 0)
					{
						XLib.XNextEvent(_display, out var ev);
						DispatchEvent(ev);
					}
				}
			}
			else
			{
				Thread.Sleep(10);
			}
		}
	}

	void DispatchEvent(XEvent ev)
	{
		try
		{
			switch (ev.type)
			{
				case XEventName.SelectionClear:
					if (ev.SelectionClearEvent.selection == _aCLIPBOARD)
						_isOwner = false;
					break;

				case XEventName.SelectionRequest:
					if (!_isOwner)
						break;
					var xsr = ev.SelectionRequestEvent;
					if (xsr.selection != _aCLIPBOARD)
						break;

					XSelectionEvent reply = default;
					reply.type = XEventName.SelectionNotify;
					reply.display = _display;
					reply.requestor = xsr.requestor;
					reply.selection = xsr.selection;
					reply.time = xsr.time;
					reply.send_event = 1;
					reply.target = xsr.target;

					if (xsr.property == None && xsr.target != _aMULTIPLE)
						xsr.property = xsr.target;

					if (reply.time != CurrentTime && reply.time < _timestamp)
					{
						reply.property = None;
					}
					else if (SwitchTargets(xsr.requestor, xsr.target, xsr.property))
					{
						reply.property = xsr.property;
					}

					if (reply.property != None)
					{
						XEvent xev = default;
						xev.SelectionEvent = reply;
						XLib.XSendEvent(_display, reply.requestor, false, IntPtr.Zero, ref xev);
						XLib.XFlush(_display);
					}
					break;
			}
		}
		catch (Exception)
		{
		}
	}

	bool SwitchTargets(IntPtr requestor, IntPtr target, IntPtr property)
	{
		if (target == _aTIMESTAMP)
		{
			XLib.XChangeProperty(_display, requestor, property, _aINTEGER, 32,
				PropertyMode.Replace, new[] { _timestamp }, 1);
			return true;
		}

		if (target == _aTARGETS)
		{
			var atoms = BuildTargetList();
			XLib.XChangeProperty(_display, requestor, property, _aATOM, 32,
				PropertyMode.Replace, atoms, atoms.Length);
			return true;
		}

		if (target == _aMULTIPLE)
		{
			if (property == None)
				return false;

			XLib.XGetWindowProperty(
				_display, requestor, property,
				IntPtr.Zero, (IntPtr)int.MaxValue, false, _aATOM_PAIR,
				out _, out var format, out var length, out _, out var atomsPtr);

			if (format != 32 || atomsPtr == IntPtr.Zero || length == IntPtr.Zero)
				return true;

			var atoms = new IntPtr[(int)length];
			Marshal.Copy(atomsPtr, atoms, 0, atoms.Length);
			XLib.XFree(atomsPtr);

			for (var i = 0; i < atoms.Length; i += 2)
				SwitchTargets(requestor, atoms[i], atoms[i + 1]);

			return true;
		}

		var targetName = XLib.GetAtomName(_display, target);

		if (TextFormats.TryGetValue(targetName, out var encoding))
			return ServeText(requestor, property, target, encoding);

		return ServeRaw(requestor, property, target, targetName);
	}

	IntPtr[] BuildTargetList()
	{
		if (_data == null)
			return new[] { _aTARGETS, _aTIMESTAMP, _aMULTIPLE };

		var list = new List<IntPtr> { _aTARGETS, _aTIMESTAMP, _aMULTIPLE };
		var hasText = _data.AvailableFormats.Contains(StandardDataFormats.Text);

		foreach (var fmt in _data.AvailableFormats)
		{
			var a = GetAtom(fmt);
			if (!list.Contains(a))
				list.Add(a);
		}

		if (hasText || _data.AvailableFormats.Any(f => TextFormats.ContainsKey(f)))
		{
			foreach (var alias in TextFormats.Keys)
			{
				var a = GetAtom(alias);
				if (!list.Contains(a))
					list.Add(a);
			}
		}

		return list.ToArray();
	}

	bool ServeText(IntPtr requestor, IntPtr property, IntPtr target, Encoding encoding)
	{
		if (_data == null)
			return false;

		string text;
		try
		{
			text = _data.GetTextAsync().GetResults();
		}
		catch
		{
			return false;
		}

		if (text == null)
			return false;

		var bytes = encoding.GetBytes(text);
		XLib.XChangeProperty(_display, requestor, property, target, 8,
			PropertyMode.Replace, bytes, bytes.Length);
		return true;
	}

	bool ServeRaw(IntPtr requestor, IntPtr property, IntPtr target, string targetName)
	{
		if (_data == null)
			return false;

		if (!_data.AvailableFormats.Contains(targetName))
			return false;

		object data;
		try
		{
			data = _data.GetDataAsync(targetName).GetResults();
		}
		catch
		{
			return false;
		}

		if (data is byte[] raw)
		{
			XLib.XChangeProperty(_display, requestor, property, target, 8,
				PropertyMode.Replace, raw, raw.Length);
			return true;
		}

		if (data is string s)
		{
			var b = Encoding.UTF8.GetBytes(s);
			XLib.XChangeProperty(_display, requestor, property, target, 8,
				PropertyMode.Replace, b, b.Length);
			return true;
		}

		return false;
	}

	IntPtr GetTimestamp()
	{
		XWindowAttributes attrs = default;
		XLib.XGetWindowAttributes(_display, _window, ref attrs);
		var savedMask = attrs.your_event_mask;

		try
		{
			XLib.XSelectInput(_display, _window, (IntPtr)PropertyChangeMask);

			var dummy = GetAtom("DUMMY_PROP_TO_GET_TIMESTAMP");
			XLib.XChangeProperty(_display, _window, dummy, _aINTEGER, 32,
				PropertyMode.Replace, new IntPtr[] { IntPtr.Zero }, 1);

			while (true)
			{
				XLib.XNextEvent(_display, out var ev);
				if (ev.type == XEventName.PropertyNotify)
					return ev.PropertyEvent.time;
			}
		}
		finally
		{
			XLib.XSelectInput(_display, _window, savedMask);
		}
	}

	public void Dispose()
	{
		_disposed = true;
		lock (_xLock)
		{
			if (_window != IntPtr.Zero)
				XLib.XDestroyWindow(_display, _window);
			if (_display != IntPtr.Zero)
				XLib.XCloseDisplay(_display);
		}
	}
}

#endif
