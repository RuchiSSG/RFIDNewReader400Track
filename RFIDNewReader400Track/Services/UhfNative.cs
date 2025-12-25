using System.Runtime.InteropServices;

namespace RFIDReaderPortal.Services
{
    public class UhfNative
    {
        [DllImport("UHFReader288.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenNetPort(
           int port,
           string ip,
           ref int comAddr,
           ref int handle
       );

        [DllImport("UHFReader288.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseNetPort(int handle);

        [DllImport("UHFReader288.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int Inventory_G2(
            ref int comAddr,
            byte adrTID,
            byte lenTID,
            byte TIDFlag,
            byte target,
            byte inAnt,
            byte scanTime,
            byte fastFlag,
            byte[] EPCList,
            ref int totalLen,
            ref int cardNum,
            int handle
        );
    }
}
