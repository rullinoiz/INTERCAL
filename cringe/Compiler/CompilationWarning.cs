namespace INTERCAL.Compiler
{
    public static class CompilationWarning
    {
        public const string W239 = "W239 WARNING HANDLER PRINTED SNIDE REMARK";
        
        // TODO: implement W276
        public const string W276 = "W276 YOU CAN'T EXPECT ME TO CHECK BACK THAT FAR";
        
        // TODO: remove the assumption that RESUME #0 is a no-op
        // the INTERCAL documentation clearly states that RESUME #0 is an error
        public const string W622 = "W622 WARNING TYPE 622 ENCOUNTERED";
    }
}