namespace Toletus.LiteNet2.Command
{
    public class RfidCard
    {
        public RfidCard(int value)
        {
            var rawWeigandHex = value.ToString("X");
            var facilityCodeHex = rawWeigandHex.Substring(rawWeigandHex.Length - 6, 2);
            var cardNumberHex = rawWeigandHex.Substring(rawWeigandHex.Length - 4);

            RawWeigand = value;
            FacilityCode = int.Parse(facilityCodeHex, System.Globalization.NumberStyles.HexNumber);
            CardNumber = int.Parse(cardNumberHex, System.Globalization.NumberStyles.HexNumber);
        }

        public int RawWeigand { get; }
        public int FacilityCode { get; }
        public int CardNumber { get; }

        public override string ToString()
        {
            return $"RawWeigand {RawWeigand} FacilityCode {FacilityCode} CardNumber {CardNumber}";
        }
    }
}