using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class ReliableMessage
{
    public static byte[] Build(Command type, byte[] payload)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)type);
        writer.Write(payload.Length);
        writer.Write(payload);
        return stream.ToArray();
    }

    public static byte[] Build<T>(Command type, T someClass) where T : class
    {
        byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(someClass));
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)type);
        writer.Write(payload.Length);
        writer.Write(payload);
        return stream.ToArray();
    }

    public static byte[] Build(Command cmd, string json)
    {
        byte[] payloadBytes = Encoding.UTF8.GetBytes(json);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)cmd);
        writer.Write(payloadBytes.Length);
        writer.Write(payloadBytes);

        return stream.ToArray();
    }

    public static (Command, string) Parse(ArraySegment<byte> data)
    {
        using var stream = new MemoryStream(data.Array, data.Offset, data.Count);
        using var reader = new BinaryReader(stream);
        var type = (Command)reader.ReadByte();
        var length = reader.ReadInt32();
        var payload = reader.ReadBytes(length);
        string json = Encoding.UTF8.GetString(payload);
        return (type, json);
    }
}
