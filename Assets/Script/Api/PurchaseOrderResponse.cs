using System;

[Serializable]
public class PurchaseOrderResponse
{
    public bool success;
    public string message;

    public int purchaseOrderId;
    public int userId;
    public int skinAndCharacterBundleId;
    public int gemCost;
    public int status;
}