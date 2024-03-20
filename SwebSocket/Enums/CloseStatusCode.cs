namespace SwebSocket
{
    public enum CloseStatusCode : ushort
    {
        NormalClosure = 1000,
        GoingAway = 1001,
        ProtocolError = 1002,
        UnsupportedData = 1003,
        InvalidFramePayloadData = 1007,
        PolicyViolation = 1008,
        MessageTooBig = 1009,
        MandatoryExt = 1010,
        InternalServerError = 1011,
    }
}