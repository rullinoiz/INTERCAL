using INTERCAL.Runtime;

namespace INTERCAL.Compiler;

public static class Constants
{
    public const string ProgramExecutionFrameName = "frame";
    public const string ProgramBeginLabel = "program_begin";
    public const string NextLabelPrefix = "label_";
    public const string ComeFromLabelPrefix = "line_";
    public const string ExitLabelName = "exit";
    public const string EvalMethodName = "Eval";
    public const string AbstainMapName = "_abstainMap";

    public const string EntryClassSignature = "internal static class Entry";
    public const string EntryMethodSignature = "internal static async Task Main(string[] args)";

    public const string EvalMethodSignature = "protected async Task " + EvalMethodName + "(" + nameof(ExecutionFrame) +
                                              " " + ProgramExecutionFrameName + ")";

    public const string LibName = nameof(Lib);
    public const string LibFail = LibName + "." + nameof(Lib.Fail);
    public const string LibRand = LibName + "." + nameof(Lib.Rand);
    public const string LibOr = LibName + "." + nameof(Lib.Or);
    public const string LibUnaryOr16 = LibName + "." + nameof(Lib.UnaryOr16);
    public const string LibUnaryOr32 = LibName + "." + nameof(Lib.UnaryOr32);
    public const string LibXor = LibName + "." + nameof(Lib.Xor);
    public const string LibUnaryXor16 = LibName + "." + nameof(Lib.UnaryXor16);
    public const string LibUnaryXor32 = LibName + "." + nameof(Lib.UnaryXor32);
    public const string LibAnd = LibName + "." + nameof(Lib.And);
    public const string LibUnaryAnd16 = LibName + "." + nameof(Lib.UnaryAnd16);
    public const string LibUnaryAnd32 = LibName + "." + nameof(Lib.UnaryAnd32);
    public const string LibMingle = LibName + "." + nameof(Lib.Mingle);
    public const string LibSelect = LibName + "." + nameof(Lib.Select);

    public const string FrameExecutionContext =
        ProgramExecutionFrameName + "." + nameof(ExecutionFrame.ExecutionContext);
    public const string ExecutionContextEvaluate =
        nameof(ExecutionFrame.ExecutionContext) + "." + nameof(ExecutionContext.Evaluate);

    public const string RuntimeEvaluate = ProgramExecutionFrameName + "." + ExecutionContextEvaluate;
    public const string RuntimeRetrieve = FrameExecutionContext + "." + nameof(ExecutionContext.Retrieve);
    public const string RuntimeStash = FrameExecutionContext + "." + nameof(ExecutionContext.Stash);
    public const string RuntimeWriteIn = FrameExecutionContext + "." + nameof(ExecutionContext.WriteIn);
    public const string RuntimeReadOut = FrameExecutionContext + "." + nameof(ExecutionContext.ReadOut);
    public const string RuntimeReDim = FrameExecutionContext + "." + nameof(ExecutionContext.ReDim);
    public const string RuntimeRemember = FrameExecutionContext + "." + nameof(ExecutionContext.Remember);
    public const string RuntimeForget = FrameExecutionContext + "." + nameof(ExecutionContext.Forget);
    public const string RuntimeIgnore = FrameExecutionContext + "." + nameof(ExecutionContext.Ignore);
    public const string RuntimeGiveUp = FrameExecutionContext + "." + nameof(ExecutionContext.GiveUp);
    public const string RuntimeResume = FrameExecutionContext + "." + nameof(ExecutionContext.Resume);
}