const { defineConfig } = require('@vue/cli-service')

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
        target: 'https://localhost:7216',
        changeOrigin: true,
        secure: false,
      },
      '/Auth': {
        target: 'https://localhost:7216',
        changeOrigin: true,
        secure: false,
      },
      '/Tables': {
        target: 'https://localhost:7216',
        changeOrigin: true,
        secure: false,
      },
      '/Reservations': {
        target: 'https://localhost:7216',
        changeOrigin: true,
        secure: false,
      },
      '/Kitchen': {
        target: 'https://localhost:7216',
        changeOrigin: true,
        secure: false,
      },
      '/Loyalty': {
        target: 'https://localhost:7216',
        changeOrigin: true,
        secure: false,
      },
      '/PublicMenu': {
        target: 'https://localhost:7216',
        changeOrigin: true,
        secure: false,
      },
      '/orderHub': {
        target: 'wss://localhost:7216',
        ws: true,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
