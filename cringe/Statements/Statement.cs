using System;
using System.Collections.Generic;
using INTERCAL.Runtime;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Compiler.Exceptions;

namespace INTERCAL.Statements
{
    public abstract partial class Statement 
	{
	#region fields
		private static readonly Dictionary<Type, int> AbstainSlots = new Dictionary<Type, int>();
		private static readonly Dictionary<Type, bool> TraditionalDict = new Dictionary<Type, bool>();
	
		/// <summary>
		/// This variable is both assigned to and read from the back end of the compiler. When EmitProgramProlog
		/// generates the abstain map it will check each statement to see if it is a target of an abstain or a
		/// reinstate. If it is then it will emit an entry for it in the abstain map and record the entry in
		/// AbstainSlot, so that later when it emits the abstain guard it can reference the right slot.
		/// </summary>
		private int AbstainSlot
		{
			get => GetStaticAbstainSlot(GetType());
			set => SetStaticAbstainSlot(GetType(), value);
		}

		public virtual int GetAbstainSlot() => AbstainSlot;
		public virtual void SetAbstainSlot(int value) => AbstainSlot = value;
		
		public bool Traditional
		{
			get => GetStaticTraditional(GetType());
			protected set => SetStaticTraditional(GetType(), value);
		}

		public virtual bool GetTraditional() => Traditional;
		public virtual void SetTraditional(bool value) => Traditional = value;

		public static int GetStaticAbstainSlot(Type type) => AbstainSlots.TryGetValue(type, out var value) 
			? value : AbstainSlots[type] = -1;
		
		public static void SetStaticAbstainSlot(Type type, int slot)
		{
			if (type == typeof(LabelStatement)) throw new CompilationException();
			AbstainSlots[type] = slot;
		}

		public static bool GetStaticTraditional(Type type) => TraditionalDict.TryGetValue(type, out var value)
			? value : TraditionalDict[type] = true;

		protected static void SetStaticTraditional(Type type, bool value)
		{
			TraditionalDict[type] = value;
		}

		/// <summary>
		/// Every statement remembers its line number in the source file.
		/// </summary>
		public int LineNumber;
		
		/// <summary>
		/// Which statement is this?
		/// </summary>
		public int StatementNumber = 0;
		
		/// <summary>
		/// Unrecognizable statements, as noted in section 9, are flagged with a splat <c>*</c> during compilation, and
		/// are not considered fatal errors unless they are encountered during execution, at which time the statement
		/// (as input at compilation time) is printed and execution is terminated.
		/// </summary>
		public bool Splatted;
		
		/// <summary>
		/// After the line label (if any), must follow one of the following statement identifiers: <c>DO</c>,
		/// <c>PLEASE</c>, or <c>PLEASE DO</c>. These may be used interchangeably to improve the aesthetics of the
		/// program. 
		/// </summary>
		/// <remarks>
		/// The identifier is then followed by either, neither, or both of the following optional parameters
		/// (qualifiers): (1) either of the character strings NOT or N'T, which causes the statement to be automatically
		/// abstained from (see section 4.4.9).
		/// </remarks>
		public bool BPlease;
		private bool BEnabled = true;

		public virtual bool GetEnabled() => BEnabled;
		public virtual void SetEnabled(bool value) => BEnabled = value;
		
		/// <summary>
		/// when execution begins, and (2) a number between 0 and 100, preceded by a double-oh-seven (%), which causes
		/// the statement to have only the specified percent chance of being executed each time it is encountered in the
		/// course of execution.
		/// </summary>
		public int Percent;

		/// <summary>
		/// The text of this statement.
		/// </summary>
		public string StatementText;

		public LabelStatement OuterLabel = null;
		
	#endregion

		public abstract void Emit(CompilationContext ctx);

		public static string ReadGroupValue(Scanner s, string group)
		{
			if (s.Current.Groups[group].Success)
				return s.Current.Groups[group].Value; 
			
			throw new ParseException(string.Format(Messages.E017, Scanner.LineNumber + 1));
		}

		public static void AssertToken(Scanner s, string val)
		{
			if (s.Current.Value != val)
				throw new ParseException(string.Format(Messages.E017, Scanner.LineNumber + 1));
		}
		/// <summary>
		/// Factory method that takes a line of input and creates a Statement object.
		/// </summary>
		/// <param name="s"></param>
		/// <returns>A constructed INTERCAL Statement.</returns>
		/// <exception cref="ParseException"></exception>
		public static Statement CreateStatement(Scanner s)
		{
			// Remember what line we started on in case the statement spans lines.
			var line = Scanner.LineNumber;
			var please = false;
			var enabled = true;
			var percent = 100;

			Statement retval = null;
			string label = null;
			uint labelNum = 0;
			
			try
			{
				// First we look to see if there is a label...
				if (s.Current.Groups["label"].Success)
				{
					label = ReadGroupValue(s, "label");
					labelNum = uint.Parse(label.Substring(1, label.Length - 2));
					s.MoveNext();
				}
				var validPrefix = false;
				
				// Next we expect either DO, PLEASE, or PLEASE DO
				if (s.Current.Value == "PLEASE")
				{
					validPrefix = true;
					please = true;
					s.MoveNext();
				}
				
				// If they've said PLEASE then they don't *have* to use DO.
				// Unless they plan on doing a DON'T or DO NOT
				if (s.Current.Value == "DO")
				{
					validPrefix = true;
					s.MoveNext();
					
					if (s.Current.Value == "NOT" || s.Current.Value == "N'T")
					{
						enabled = false;
						s.MoveNext();
					}
				}
				// Finally the user might put a %50 here.
				// Note that even if the statement is disabled we need to remember the % on the off chance that the
				// statement gets enabled later.
				if (s.Current.Value == "%")
				{
					s.MoveNext();
					var p = ReadGroupValue(s, "digits");
					percent = int.Parse(p);
					s.MoveNext();
				}
				
				// Here we parse out the statement prefix. Easier to do it here than break out a separate function.
				while (s.Current.Groups["prefix"].Success)
				{
					switch (s.Current.Value)
					{
						case "DO": 
							validPrefix = true; 
							break;
						case "PLEASE":
							validPrefix = true;
							please = true;
							break;
						case "NOT":
						case "N'T":
							enabled = false;
							break;
						case "%":
							s.MoveNext();
							var p = ReadGroupValue(s, "digits");
							percent = int.Parse(p);
							break;
					}
					s.MoveNext();
				}
				
				if (!validPrefix) 
					throw new ParseException(string.Format(Messages.E017, Scanner.LineNumber + 1));

				if (s.Current.Groups["statement"].Success)
					// We are looking at the beginning of a statement
					switch (s.Current.Value)
					{
						case AbstainStatement.Token:		retval = new AbstainStatement(s);	break;
						case ReadOutStatement.Token:		retval = new ReadOutStatement(s);	break;
						case WriteInStatement.Token:		retval = new WriteInStatement(s);	break;
						case ComeFromStatement.Token:		retval = new ComeFromStatement(s);	break;	
						case ReinstateStatement.Token:		retval = new ReinstateStatement(s);	break;
						case StashStatement.Token:			retval = new StashStatement(s);		break;
						case ResumeStatement.Token:			retval = new ResumeStatement(s);	break;
						case ForgetStatement.Token:			retval = new ForgetStatement(s);	break;
						case IgnoreStatement.Token:			retval = new IgnoreStatement(s);	break;
						case RememberStatement.Token:		retval = new RememberStatement(s);	break;
						case RetrieveStatement.Token:		retval = new RetrieveStatement(s);	break;
						case GiveUpStatement.Token:			retval = new GiveUpStatement();		break;
						case TryAgainStatement.Token:		retval = new TryAgainStatement();	break;
					}
				else if (s.Current.Groups["label"].Success)
					retval = new NextStatement(s);
				else if (s.Current.Groups["var"].Success)
					retval = new CalculateStatement(s);
				else
					throw new ParseException(string.Format(Messages.E017, Scanner.LineNumber + 1));

				// Move on to what should be the beginning of the next statement
				s.MoveNext();
			}
			catch (ParseException)
			{
                //Console.WriteLine(p.Message);
				if (retval != null) 
					retval.Splatted = true;
				else retval = new NonsenseStatement();

				s.Panic();
			}
			
			retval.LineNumber = line;
			retval.BEnabled = enabled;
			retval.BPlease = please;
			retval.Percent = percent;
			
			if (TryAgainStatement.TryAgainLine > 0)
				throw new CompilationException(Messages.E993);

			if (retval is TryAgainStatement)
				TryAgainStatement.TryAgainLine = line;
			
			// Note that even badly formed statements get their labels set.
			// This way you can still jump to them (though that will cause an exception).
			if (label == null || labelNum == 0) return retval;
			
			var labelStatement = new LabelStatement
			{
				LabelNumber = labelNum,
				Statement = retval,
				LineNumber = line
			};

			retval.OuterLabel = labelStatement;

			return labelStatement;
		}
	}
}