-if class com.habiham.tracking.data.model.LoginRequest
-keepnames class com.habiham.tracking.data.model.LoginRequest
-if class com.habiham.tracking.data.model.LoginRequest
-keep class com.habiham.tracking.data.model.LoginRequestJsonAdapter {
    public <init>(com.squareup.moshi.Moshi);
}
