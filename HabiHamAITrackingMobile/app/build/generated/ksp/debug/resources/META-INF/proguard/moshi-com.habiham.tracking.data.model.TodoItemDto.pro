-if class com.habiham.tracking.data.model.TodoItemDto
-keepnames class com.habiham.tracking.data.model.TodoItemDto
-if class com.habiham.tracking.data.model.TodoItemDto
-keep class com.habiham.tracking.data.model.TodoItemDtoJsonAdapter {
    public <init>(com.squareup.moshi.Moshi);
}
-if class com.habiham.tracking.data.model.TodoItemDto
-keepnames class kotlin.jvm.internal.DefaultConstructorMarker
-if class com.habiham.tracking.data.model.TodoItemDto
-keepclassmembers class com.habiham.tracking.data.model.TodoItemDto {
    public synthetic <init>(java.lang.String,java.lang.String,java.lang.String,java.lang.String,java.lang.String,java.lang.String,boolean,java.lang.String,int,kotlin.jvm.internal.DefaultConstructorMarker);
}
