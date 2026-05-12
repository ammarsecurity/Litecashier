const { defineConfig } = require('@vue/cli-service')

/** Same origin as `VUE_APP_API_URL` in `.env` (used by devServer proxy). */
function parseApiTargets() {
  const raw = (process.env.VUE_APP_API_URL || 'https://localhost:7216/').trim()
  try {
    const u = new URL(raw.endsWith('/') ? raw : `${raw}/`)
    const httpOrigin = `${u.protocol}//${u.host}`
    const wsOrigin = `${u.protocol === 'https:' ? 'wss:' : 'ws:'}//${u.host}`
    return { httpOrigin, wsOrigin }
  } catch {
    return { httpOrigin: 'https://localhost:7216', wsOrigin: 'wss://localhost:7216' }
  }
}

const { httpOrigin: apiTarget, wsOrigin: wsTarget } = parseApiTargets()

module.exports = defineConfig({
  transpileDependencies: true,

  // 🔴 الحل هنا
  // لازم يكون "/" حتى لا يضيف /menu/ قبل static
  publicPath: '/',

  outputDir: 'dist',
  assetsDir: 'static',
  productionSourceMap: false,

  configureWebpack: {
    optimization: {
      splitChunks: {
        chunks: 'all',
      },
    },
  },

  devServer: {
    port: 8000,
    https: false,
    proxy: {
      '/Admin': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
      '/Auth': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
      '/Tables': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
      '/Reservations': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
      '/Kitchen': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
      '/Loyalty': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
      '/PublicMenu': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
      '/orderHub': {
        target: wsTarget,
        ws: true,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
