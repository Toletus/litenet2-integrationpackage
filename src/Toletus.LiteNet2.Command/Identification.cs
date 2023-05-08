using Toletus.LiteNet2.Command.Enums;

namespace Toletus.LiteNet2.Command
{
    public class Identification
    {
        public Identification(IdentificationDevice device, int data)
        {
            Device = device;
            Data = data;

            if (Device == IdentificationDevice.Rfid)
                RfidCard = new RfidCard(Data);
        }

        public IdentificationDevice Device { get; }
        public int Data { get; }
        public int Id => RfidCard?.RawWeigand ?? Data;
        public RfidCard RfidCard { get; }
        public byte[] EmbededTemplate { get; }

        public override string ToString()
        {
            return $"Device {Device} Data {Data} RfidCard {RfidCard}";
        }
    }
}
