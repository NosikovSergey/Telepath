using Telepath.Routing;

namespace Telepath.Tests;

public class CallbackDataTests
{
    #region Test types

    [CallbackPrefix("user")]
    private class UserData : CallbackData
    {
        public int Id { get; set; }
        public string Action { get; set; } = "";
        public bool Active { get; set; }
    }

    [CallbackPrefix("empty")]
    private class EmptyData : CallbackData { }

    [CallbackPrefix("nums")]
    private class NumericData : CallbackData
    {
        public long LongVal { get; set; }
        public double DoubleVal { get; set; }
        public float FloatVal { get; set; }
        public decimal DecimalVal { get; set; }
    }

    [CallbackPrefix("dates")]
    private class DateData : CallbackData
    {
        public DateTimeOffset Timestamp { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan Duration { get; set; }
    }

    [CallbackPrefix("guid")]
    private class GuidData : CallbackData
    {
        public Guid Id { get; set; }
    }

    private class NoPrefixData : CallbackData { }

    [CallbackPrefix("priv")]
    private class PrivateCtorData : CallbackData
    {
        private PrivateCtorData() { }
        public static PrivateCtorData Create(int value = 0) => new() { Value = value };
        public int Value { get; set; }
    }

    [CallbackPrefix("chardata")]
    private class CharData : CallbackData
    {
        public char Letter { get; set; }
    }

    [CallbackPrefix("edge")]
    private class EdgeData : CallbackData
    {
        public string Value { get; set; } = "";
    }

    [CallbackPrefix("strfirst")]
    private class StringFirstData : CallbackData
    {
        public string Value { get; set; } = "";
        public int Id { get; set; }
    }

    [CallbackPrefix("refnull")]
    private class RefNullableData : CallbackData
    {
        public string? Name { get; set; }
    }

    [CallbackPrefix("boundary")]
    private class BoundaryData : CallbackData
    {
        public int IntVal { get; set; }
        public long LongVal { get; set; }
    }

    [CallbackPrefix("negspan")]
    private class NegSpanData : CallbackData
    {
        public TimeSpan Duration { get; set; }
    }

    private enum UserAction { Ban = 1, Mute = 2, Kick = 3 }

    [CallbackPrefix("action")]
    private class ActionData : CallbackData
    {
        public UserAction Action { get; set; }
        public int UserId { get; set; }
    }

    [CallbackPrefix("nullable")]
    private class NullableData : CallbackData
    {
        public int? MaybeId { get; set; }
        public bool? MaybeFlag { get; set; }
    }

    #endregion

    // Serialize

    [Fact]
    public void Serialize_WithFields_ReturnsCorrectString()
    {
        var data = new UserData { Id = 42, Action = "ban", Active = true };
        Assert.Equal("user:g:ban:1", data.Serialize());
    }

    [Fact]
    public void Serialize_NoFields_ReturnsPrefixOnly()
    {
        Assert.Equal("empty", new EmptyData().Serialize());
    }

    [Fact]
    public void Serialize_DateTimeOffset_IsUnixTimestamp()
    {
        var data = new DateData { Timestamp = DateTimeOffset.FromUnixTimeSeconds(1700000000) };
        Assert.Contains("1r31eq", data.Serialize());
    }

    // TryDeserialize

    [Fact]
    public void TryDeserialize_ValidData_ReturnsTrueAndParsesFields()
    {
        var ok = CallbackData.TryDeserialize<UserData>("user:g:ban:1", out var result);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("ban", result.Action);
        Assert.True(result.Active);
    }

    [Fact]
    public void TryDeserialize_WrongPrefix_ReturnsFalse()
    {
        var ok = CallbackData.TryDeserialize<UserData>("admin:42:ban:True", out var result);

        Assert.False(ok);
        Assert.Null(result);
    }

    [Fact]
    public void TryDeserialize_PrefixPartialMatch_ReturnsFalse()
    {
        var ok = CallbackData.TryDeserialize<UserData>("userextended:42:ban:True", out var result);

        Assert.False(ok);
        Assert.Null(result);
    }

    [Fact]
    public void TryDeserialize_TooFewSegments_ReturnsFalse()
    {
        var ok = CallbackData.TryDeserialize<UserData>("user:42:ban", out var result);

        Assert.False(ok);
        Assert.Null(result);
    }

    [Fact]
    public void TryDeserialize_TooManySegments_ReturnsFalse()
    {
        var ok = CallbackData.TryDeserialize<UserData>("user:42:ban:True:extra", out var result);

        Assert.False(ok);
        Assert.Null(result);
    }

    [Fact]
    public void TryDeserialize_WrongFieldType_ReturnsFalse()
    {
        var ok = CallbackData.TryDeserialize<UserData>("user:notanint:ban:True", out var result);

        Assert.False(ok);
        Assert.Null(result);
    }

    [Fact]
    public void TryDeserialize_NoFields_ExactPrefixMatch_ReturnsTrue()
    {
        var ok = CallbackData.TryDeserialize<EmptyData>("empty", out var result);

        Assert.True(ok);
        Assert.NotNull(result);
    }

    [Fact]
    public void TryDeserialize_NoFields_ExtraSegment_ReturnsFalse()
    {
        var ok = CallbackData.TryDeserialize<EmptyData>("empty:extra", out var result);

        Assert.False(ok);
        Assert.Null(result);
    }

    // Deserialize

    [Fact]
    public void Deserialize_ValidData_ReturnsInstance()
    {
        var result = CallbackData.Deserialize<UserData>("user:g:ban:1");
        Assert.Equal(42, result.Id);
    }

    [Fact]
    public void Deserialize_InvalidData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CallbackData.Deserialize<UserData>("bad:data"));
    }

    // Missing attribute

    [Fact]
    public void Serialize_MissingAttribute_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => new NoPrefixData().Serialize());
    }

    [Fact]
    public void TryDeserialize_MissingAttribute_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => CallbackData.TryDeserialize<NoPrefixData>("anything", out _));
    }

    [Fact]
    public void Deserialize_MissingAttribute_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => CallbackData.Deserialize<NoPrefixData>("anything"));
    }

    [Fact]
    public void CreateFilter_MissingAttribute_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => CallbackData.CreateFilter<NoPrefixData>());
    }

    // Private constructor — TryDeserialize/Deserialize are compile-time protected via new() constraint

    [Fact]
    public void Serialize_PrivateConstructor_ReturnsCorrectString()
    {
        Assert.Equal("priv:g", PrivateCtorData.Create(42).Serialize());
    }

    [Fact]
    public void CreateFilter_PrivateConstructor_MatchesPrefix()
    {
        var filter = CallbackData.CreateFilter<PrivateCtorData>();

        Assert.True(filter("priv:g"));
        Assert.False(filter("other:g"));
    }

    // CreateFilter

    [Fact]
    public void CreateFilter_MatchingData_ReturnsTrue()
    {
        var filter = CallbackData.CreateFilter<UserData>();
        Assert.True(filter("user:g:ban:1"));
    }

    [Fact]
    public void CreateFilter_WrongPrefix_ReturnsFalse()
    {
        var filter = CallbackData.CreateFilter<UserData>();
        Assert.False(filter("admin:g:ban:1"));
    }

    [Fact]
    public void CreateFilter_PrefixPartialMatch_ReturnsFalse()
    {
        var filter = CallbackData.CreateFilter<UserData>();
        Assert.False(filter("userextended:g:ban:1"));
    }

    [Fact]
    public void CreateFilter_NoFields_MatchesExactPrefix()
    {
        var filter = CallbackData.CreateFilter<EmptyData>();
        Assert.True(filter("empty"));
        Assert.False(filter("empty:extra"));
    }

    [Fact]
    public void CreateFilter_WithPredicate_FiltersOnFieldValue()
    {
        var filter = CallbackData.CreateFilter<UserData>(x => x.Action == "ban");

        Assert.True(filter("user:g:ban:1"));
        Assert.False(filter("user:g:kick:1"));
    }

    // Edge cases

    [Fact]
    public void RoundTrip_StringWithColon_PreservesValue()
    {
        var original = new EdgeData { Value = "hello:world" };
        var result = CallbackData.Deserialize<EdgeData>(original.Serialize());
        Assert.Equal("hello:world", result.Value);
    }

    [Fact]
    public void RoundTrip_StringWithColon_AsNonLastField_PreservesValue()
    {
        var original = new StringFirstData { Value = "hello:world", Id = 42 };
        var result = CallbackData.Deserialize<StringFirstData>(original.Serialize());
        Assert.Equal("hello:world", result.Value);
        Assert.Equal(42, result.Id);
    }

    [Fact]
    public void RoundTrip_EmptyString_PreservesValue()
    {
        var original = new EdgeData { Value = "" };
        var result = CallbackData.Deserialize<EdgeData>(original.Serialize());
        Assert.Equal("", result.Value);
    }

    [Fact]
    public void RoundTrip_NullableReferenceString_WithNull_PreservesNull()
    {
        var original = new RefNullableData { Name = null };
        var result = CallbackData.Deserialize<RefNullableData>(original.Serialize());
        Assert.Null(result.Name);
    }

    [Fact]
    public void RoundTrip_NullableReferenceString_WithValue_PreservesValue()
    {
        var original = new RefNullableData { Name = "hello" };
        var result = CallbackData.Deserialize<RefNullableData>(original.Serialize());
        Assert.Equal("hello", result.Name);
    }

    [Fact]
    public void RoundTrip_Enum_UndefinedValue_ReturnsRawValue()
    {
        var ok = CallbackData.TryDeserialize<ActionData>("action:1b:g", out var result);
        Assert.True(ok);
        Assert.Equal((UserAction)99, result!.Action);
    }

    [Fact]
    public void RoundTrip_NegativeTimeSpan_PreservesValue()
    {
        var original = new NegSpanData { Duration = TimeSpan.FromHours(-3) };
        var result = CallbackData.Deserialize<NegSpanData>(original.Serialize());
        Assert.Equal(original.Duration, result.Duration);
    }

    [Fact]
    public void RoundTrip_IntAtBoundary_PreservesValue()
    {
        var original = new BoundaryData { IntVal = int.MaxValue, LongVal = long.MinValue };
        var result = CallbackData.Deserialize<BoundaryData>(original.Serialize());
        Assert.Equal(int.MaxValue, result.IntVal);
        Assert.Equal(long.MinValue, result.LongVal);
    }

    // Round-trip

    [Fact]
    public void RoundTrip_PrimitiveTypes_PreservesValues()
    {
        var original = new UserData { Id = 42, Action = "ban", Active = true };
        var result = CallbackData.Deserialize<UserData>(original.Serialize());

        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.Action, result.Action);
        Assert.Equal(original.Active, result.Active);
    }

    [Fact]
    public void RoundTrip_NumericTypes_PreservesValues()
    {
        var original = new NumericData
        {
            LongVal = long.MaxValue,
            DoubleVal = 3.14,
            FloatVal = 1.5f,
            DecimalVal = 99.99m
        };
        var result = CallbackData.Deserialize<NumericData>(original.Serialize());

        Assert.Equal(original.LongVal, result.LongVal);
        Assert.Equal(original.DoubleVal, result.DoubleVal);
        Assert.Equal(original.FloatVal, result.FloatVal);
        Assert.Equal(original.DecimalVal, result.DecimalVal);
    }

    [Fact]
    public void RoundTrip_Guid_PreservesValue()
    {
        var original = new GuidData { Id = Guid.NewGuid() };
        var result = CallbackData.Deserialize<GuidData>(original.Serialize());

        Assert.Equal(original.Id, result.Id);
    }

    // Enum

    [Fact]
    public void Serialize_Enum_StoresAsNumber()
    {
        var data = new ActionData { Action = UserAction.Ban, UserId = 42 };
        Assert.Equal("action:1:g", data.Serialize());
    }

    [Fact]
    public void RoundTrip_Enum_PreservesValue()
    {
        var original = new ActionData { Action = UserAction.Mute, UserId = 99 };
        var result = CallbackData.Deserialize<ActionData>(original.Serialize());

        Assert.Equal(original.Action, result.Action);
        Assert.Equal(original.UserId, result.UserId);
    }

    // Nullable

    [Fact]
    public void RoundTrip_Nullable_WithValue_PreservesValue()
    {
        var original = new NullableData { MaybeId = 42, MaybeFlag = true };
        var result = CallbackData.Deserialize<NullableData>(original.Serialize());

        Assert.Equal(42, result.MaybeId);
        Assert.True(result.MaybeFlag);
    }

    [Fact]
    public void RoundTrip_Nullable_WithNull_PreservesNull()
    {
        var original = new NullableData { MaybeId = null, MaybeFlag = null };
        var result = CallbackData.Deserialize<NullableData>(original.Serialize());

        Assert.Null(result.MaybeId);
        Assert.Null(result.MaybeFlag);
    }

    [Fact]
    public void RoundTrip_Nullable_Mixed_PreservesValues()
    {
        var original = new NullableData { MaybeId = 7, MaybeFlag = null };
        var result = CallbackData.Deserialize<NullableData>(original.Serialize());

        Assert.Equal(7, result.MaybeId);
        Assert.Null(result.MaybeFlag);
    }

    // Date types

    [Fact]
    public void RoundTrip_DateTypes_PreservesValues()
    {
        var original = new DateData
        {
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1700000000),
            Date = new DateOnly(2024, 1, 15),
            Time = new TimeOnly(10, 30, 0),
            DateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Duration = TimeSpan.FromHours(2)
        };
        var result = CallbackData.Deserialize<DateData>(original.Serialize());

        Assert.Equal(original.Timestamp, result.Timestamp);
        Assert.Equal(original.Date, result.Date);
        Assert.Equal(original.Time, result.Time);
        Assert.Equal(original.DateTime, result.DateTime);
        Assert.Equal(original.Duration, result.Duration);
    }

    // Char

    [Fact]
    public void Serialize_Char_StoresAsBase62()
    {
        var data = new CharData { Letter = 'A' };
        Assert.Equal("chardata:13", data.Serialize()); // 'A' = 65 = 1*62+3
    }

    [Fact]
    public void RoundTrip_Char_PreservesValue()
    {
        var original = new CharData { Letter = '€' };
        var result = CallbackData.Deserialize<CharData>(original.Serialize());

        Assert.Equal('€', result.Letter);
    }

    // String escaping — backslash

    [Fact]
    public void RoundTrip_StringWithBackslash_PreservesValue()
    {
        var original = new EdgeData { Value = @"hello\world" };
        var result = CallbackData.Deserialize<EdgeData>(original.Serialize());

        Assert.Equal(@"hello\world", result.Value);
    }

    [Fact]
    public void RoundTrip_StringWithBackslashAndColon_PreservesValue()
    {
        var original = new EdgeData { Value = @"a\b:c" };
        var result = CallbackData.Deserialize<EdgeData>(original.Serialize());

        Assert.Equal(@"a\b:c", result.Value);
    }

    [Fact]
    public void RoundTrip_StringWithBackslashAndColon_AsNonLastField_PreservesValue()
    {
        var original = new StringFirstData { Value = @"a\:b", Id = 7 };
        var result = CallbackData.Deserialize<StringFirstData>(original.Serialize());

        Assert.Equal(@"a\:b", result.Value);
        Assert.Equal(7, result.Id);
    }
}
