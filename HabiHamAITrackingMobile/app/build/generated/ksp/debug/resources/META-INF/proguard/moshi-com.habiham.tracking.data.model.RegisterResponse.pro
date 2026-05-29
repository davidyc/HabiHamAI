-if class com.habiham.tracking.data.model.RegisterResponse
-keepnames class com.habiham.tracking.data.model.RegisterResponse
-if class com.habiham.tracking.data.model.RegisterResponse
-keep class com.habiham.tracking.data.model.RegisterResponseJsonAdapter {
    public <init>(com.squareup.moshi.Moshi);
}
-if class com.habiham.tracking.data.model.RegisterResponse
-keepnames class kotlin.jvm.internal.DefaultConstructorMarker
-if class com.habiham.tracking.data.model.RegisterResponse
-keepclassmembers class com.habiham.tracking.data.model.RegisterResponse {
    public synthetic <init>(java.lang.String,java.lang.String,int,kotlin.jvm.internal.DefaultConstructorMarker);
}
