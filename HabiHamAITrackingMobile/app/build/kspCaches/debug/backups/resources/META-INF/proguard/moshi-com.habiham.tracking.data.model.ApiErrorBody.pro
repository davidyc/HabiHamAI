-if class com.habiham.tracking.data.model.ApiErrorBody
-keepnames class com.habiham.tracking.data.model.ApiErrorBody
-if class com.habiham.tracking.data.model.ApiErrorBody
-keep class com.habiham.tracking.data.model.ApiErrorBodyJsonAdapter {
    public <init>(com.squareup.moshi.Moshi);
}
-if class com.habiham.tracking.data.model.ApiErrorBody
-keepnames class kotlin.jvm.internal.DefaultConstructorMarker
-if class com.habiham.tracking.data.model.ApiErrorBody
-keepclassmembers class com.habiham.tracking.data.model.ApiErrorBody {
    public synthetic <init>(java.lang.String,int,kotlin.jvm.internal.DefaultConstructorMarker);
}
