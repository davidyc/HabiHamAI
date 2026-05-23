package com.habiham.mobile

import android.app.Application
import com.habiham.mobile.data.prefs.SessionStore
import com.habiham.mobile.data.repository.AuthRepository
import com.habiham.mobile.data.repository.BikeRepository
import com.habiham.mobile.data.repository.WorkoutsRepository
import org.osmdroid.config.Configuration
import java.io.File

class HabiHamApplication : Application() {
    lateinit var sessionStore: SessionStore
        private set

    lateinit var authRepository: AuthRepository
        private set

    lateinit var workoutsRepository: WorkoutsRepository
        private set

    lateinit var bikeRepository: BikeRepository
        private set

    override fun onCreate() {
        super.onCreate()
        Configuration.getInstance().userAgentValue = packageName
        val osmBase = File(cacheDir, "osmdroid").apply { mkdirs() }
        Configuration.getInstance().osmdroidBasePath = osmBase
        Configuration.getInstance().osmdroidTileCache = File(osmBase, "tiles").apply { mkdirs() }

        sessionStore = SessionStore(this)
        authRepository = AuthRepository(sessionStore)
        workoutsRepository = WorkoutsRepository()
        bikeRepository = BikeRepository()
    }
}
