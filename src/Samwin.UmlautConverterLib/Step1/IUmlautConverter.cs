namespace Samwin.UmlautConverterLib.Step1
{
    public interface IUmlautConverter
    {
        /// <summary>
        /// Converts the input string by replacing recognized sequences with umlauts.
        /// </summary>
        string Convert(string input);
    }
}