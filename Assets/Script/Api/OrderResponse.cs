using System;

[Serializable]
public class OrderResponse
{
    public int shopOrderId;
    public string qrCode;
    public string status;
    public bool isSuccess;
    public string checkoutUrl;

    public string expiredAt;
    public string bundleName;
}