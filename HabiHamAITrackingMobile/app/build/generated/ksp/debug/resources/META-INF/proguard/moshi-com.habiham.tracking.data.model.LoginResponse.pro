-if class com.habiham.tracking.data.model.LoginResponse
-keepnames class com.habiham.tracking.data.model.LoginResponse
-if class com.habiham.tracking.data.model.LoginResponse
-keep class com.habiham.tracking.data.model.LoginResponseJsonAdapter {
    public <init>(com.squareup.moshi.Moshi);
}
-if class com.habiham.tracking.data.model.LoginResponse
-keepnames class kotlin.jvm.internal.DefaultConstructorMarker
-if class com.habiham.tracking.data.model.LoginResponse
-keepclassmembers class com.habiham.tracking.data.model.LoginResponse {
    public synthetic <init>(java.lang.String,java.lang.String,int,kotlin.jvm.internal.DefaultConstructorMarker);
}
