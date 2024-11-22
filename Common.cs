using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public static class SafeInput {
	private static readonly string ErrorMsg = "Что-то не так, попробуй еще раз: ";

	private static void Write(string? msg) {
		if(msg != null)
			Console.Write(msg);
	}

	public static int Uint(string? msg, int? max) {
		int u;

		Write(msg);
		
		while(!(Int32.TryParse(
			Console.ReadLine(), out u)
			&& u >= 0 
			&& (max == null || u <= max))
		) {
			Console.Write(ErrorMsg);
		}
		return (int)u;
	}

	public static ulong Ulong(string? msg, long? max) {
		long u;

		Write(msg);

		while(!(Int64.TryParse(
			Console.ReadLine(), out u)
			&& u >= 0
			&& (max == null || u <= max))
		) {
			Console.Write(ErrorMsg);
		}
		return (ulong)u;
	}

	public static string String(string? msg) {
		Write(msg);
		return Console.ReadLine() ?? "";
	}

	public static bool Bool(string? msg) {
		Write(msg);
		bool u;

		while(!(Boolean.TryParse(
			Console.ReadLine(), out u))
		) {
			Console.Write(ErrorMsg);
		}
		return u;
	}

	public static DateTime Date(string? msg) {
		DateTime date;
		Write(msg);

		while(!DateTime.TryParse(Console.ReadLine(), out date)) {
			Console.Write(ErrorMsg);
		}
		return date;
	}
}

public static class Refl {
	private static BindingFlags _flags =
		BindingFlags.DeclaredOnly |
		BindingFlags.Public |
		BindingFlags.Instance;

	public static void Invoke<T>(T? obj, string mName) {
		var m = typeof(T).GetMethod(mName);
		m.Invoke(obj, null);
	}

	public static void PrintMethods<T>() {
		foreach(var m in typeof(T).GetMethods(_flags)) {
			Console.Write(m.Name + "; ");
		}
		Console.WriteLine();
	}

	public static void ChooseMethod<T>(T? obj, string? msg) {
		if(msg != null) {
			Console.WriteLine(msg);
		}
		PrintMethods<T>();
		string mName = SafeInput.String(null);
		Invoke(obj, mName);
	}
}

public record MenuEntry(string desc, Action action);

public class Menu: IEnumerable {
	private List<MenuEntry> _entries = new();
	private int ExitIndex => _entries.Count;

	public void Add(string desc, Action action) {
		_entries.Add(new(desc, action));
	}

	public void Print() {
		Console.WriteLine();
		for(var i = 0; i < ExitIndex; i++) {
			Console.WriteLine(i.ToString() + ". " + _entries[i].desc);
		}
		Console.WriteLine(ExitIndex.ToString() + ". Выйти");
	}

	public void Run() {
		int index = 0;

		Print();
		while(true) {
			index = SafeInput.Uint("Действие: ", ExitIndex);
			if (index == ExitIndex) {
				break;
			}
			_entries[index].action();
		}
	}

	public IEnumerator<MenuEntry> GetEnumerator() {
		return _entries.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
}
