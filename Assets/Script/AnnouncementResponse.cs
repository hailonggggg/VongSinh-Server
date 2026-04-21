using System;

[Serializable]
public class AnnouncementResponse
{
    public int announcementId;
    public string title;
    public string content;
    public string type;
    public string status;
    public DateTime startDate;
    public DateTime endDate;
    public int createdBy;
    public DateTime createdAt;

    public int? updatedBy; 
    public DateTime? updatedAt; 
}