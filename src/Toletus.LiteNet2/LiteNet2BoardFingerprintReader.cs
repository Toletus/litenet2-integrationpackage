using System;
using System.Threading.Tasks;
using Toletus.SM25;
using Toletus.SM25.Base;
using Toletus.SM25.Command.Enums;

namespace Toletus.LiteNet2;

public partial class LiteNet2Board
{
    public void CreateFingerprintReaderAndTest()
    {
        Task.Run(CreateFingerprintReader);
    }

    public SM25Reader CreateFingerprintReader()
    {
        FingerprintReader = new SM25Reader(Ip);
        FingerprintReader.OnConnectionStateChanged += FingerprintReaderConnectionStateChanged;

        FingerprintReader.TestFingerprintReaderConnection();

        return FingerprintReader;
    }

    private void FingerprintReaderConnectionStateChanged(SM25ConnectionStatus connectionStatus)
    {
        if (connectionStatus == SM25ConnectionStatus.Closed) return;

        try
        {
            if (!HasFingerprintReader && connectionStatus == SM25ConnectionStatus.Connected)
                HasFingerprintReader = true;

            var ret = FingerprintReader.Sync.TestConnection();

            HasFingerprintReader = (ret != null && ret.ReturnCode != ReturnCodes.ERR_UNDEFINED);

            if (HasFingerprintReader) FingerprintReader.Sync.SetFingerTimeOut(60);
            FingerprintReader.Close();
        }
        catch (FingerprintConnectionException ex)
        {
            HasFingerprintReader = false;
            Log?.Invoke($"Fingerprint connection exception: " + ex.Message);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"{nameof(FingerprintReaderConnectionStateChanged)} Exception " + ex.Message);
            throw;
        }
        finally
        {
            if (FingerprintReader != null)
                FingerprintReader.OnConnectionStateChanged -= FingerprintReaderConnectionStateChanged;
            
            OnFingerprintReaderConnected?.Invoke(this, HasFingerprintReader);
        }
    }
}