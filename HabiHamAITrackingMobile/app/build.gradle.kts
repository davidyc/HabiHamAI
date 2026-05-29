import java.util.Properties

plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.ksp)
}

fun loadProperties(fileName: String): Properties = Properties().apply {
    val file = rootProject.file(fileName)
    if (file.exists()) {
        file.inputStream().use { load(it) }
    }
}

fun quoteBuildConfigUrl(url: String): String =
    "\"${url.trim().replace("\"", "\\\"")}\""

val productionApiBaseUrl = "https://habihamai.onrender.com"

val localProperties = loadProperties("local.properties")
val apiConfigProperties = loadProperties("api-config.properties")

fun firstNonBlankUrl(vararg candidates: String?): String =
    candidates.firstOrNull { !it.isNullOrBlank() }?.trim() ?: productionApiBaseUrl

android {
    namespace = "com.habiham.tracking"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.habiham.tracking"
        minSdk = 26
        targetSdk = 35
        versionCode = 1
        versionName = "1.0.0"
    }

    buildTypes {
        debug {
            isDebuggable = true
            val debugUrl = firstNonBlankUrl(localProperties.getProperty("habiHam.apiBaseUrl"))
            buildConfigField(
                "String",
                "DEFAULT_API_BASE_URL",
                quoteBuildConfigUrl(debugUrl),
            )
        }
        release {
            val releaseUrl = firstNonBlankUrl(
                apiConfigProperties.getProperty("releaseApiBaseUrl"),
                project.findProperty("habiHam.releaseApiBaseUrl")?.toString(),
                localProperties.getProperty("habiHam.releaseApiBaseUrl"),
            )
            buildConfigField(
                "String",
                "DEFAULT_API_BASE_URL",
                quoteBuildConfigUrl(releaseUrl),
            )
            signingConfig = signingConfigs.getByName("debug")
        }
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }

    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
        }
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.core.splashscreen)
    implementation(libs.androidx.security.crypto)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.lifecycle.viewmodel.compose)
    implementation(libs.androidx.lifecycle.runtime.compose)
    implementation(libs.androidx.activity.compose)
    implementation(platform(libs.androidx.compose.bom))
    implementation(libs.androidx.compose.ui)
    implementation(libs.androidx.compose.ui.graphics)
    implementation(libs.androidx.compose.ui.tooling.preview)
    implementation(libs.androidx.compose.material3)
    implementation(libs.androidx.compose.material.icons)
    implementation(libs.retrofit)
    implementation(libs.retrofit.moshi)
    implementation(libs.okhttp)
    implementation(libs.okhttp.logging)
    implementation(libs.moshi)
    ksp(libs.moshi.codegen)
    implementation(libs.datastore)
    debugImplementation(libs.androidx.compose.ui.tooling.preview)
}
