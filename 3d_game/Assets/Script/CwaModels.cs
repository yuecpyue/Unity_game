using System;
using System.Collections.Generic;

/// <summary>
/// 中央氣象署 (CWA) 開放資料平臺 O-A0003-001 API 之 JSON 資料模型
/// 嚴格對應官方四層結構，以利 JsonUtility 進行序列化與反序列化
/// </summary>
[Serializable]
public class CwaResponse
{
    public Records records;
}

[Serializable]
public class Records
{
    public List<Station> Station;
}

[Serializable]
public class Station
{
    public string StationName;
    public ObsTime ObsTime;
    public WeatherElement WeatherElement;
}

[Serializable]
public class ObsTime
{
    public string DateTime;
}

[Serializable]
public class WeatherElement
{
    public string Weather;
    public float AirTemperature;
    public float RelativeHumidity;
}
