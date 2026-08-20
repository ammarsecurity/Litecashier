<template>
  <div
    v-if="items.length"
    class="ann-slider"
    @mouseenter="pause"
    @mouseleave="resume"
  >
    <div class="ann-slider-viewport">
      <div class="ann-slider-track" :style="trackStyle">
        <article
          v-for="(item, idx) in items"
          :key="item.id || idx"
          class="ann-slide"
          :class="{
            'ann-slide--clickable': !!item.linkUrl,
            'ann-slide--no-image': !item.imageUrl,
          }"
          @click="openLink(item)"
        >
          <div
            v-if="item.imageUrl"
            class="ann-slide-media"
            :style="{ backgroundImage: `url(${item.imageUrl})` }"
            aria-hidden="true"
          />
          <div class="ann-slide-shade" aria-hidden="true" />
          <div class="ann-slide-content">
            <span class="ann-slide-badge">{{ $t("announcementBadge") || "إعلان" }}</span>
            <h3 class="ann-slide-title">{{ item.title }}</h3>
            <p v-if="item.body" class="ann-slide-text">{{ item.body }}</p>
            <span v-if="item.linkUrl" class="ann-slide-cta">
              {{ $t("announcementLearnMore") || "اعرف المزيد" }}
              <b-icon icon="arrow-left" class="ann-slide-cta-ico"></b-icon>
            </span>
          </div>
        </article>
      </div>

      <template v-if="items.length > 1">
        <button
          type="button"
          class="ann-nav ann-nav--prev"
          :aria-label="$t('previous') || 'السابق'"
          @click.stop="prev"
        >
          <b-icon icon="chevron-right"></b-icon>
        </button>
        <button
          type="button"
          class="ann-nav ann-nav--next"
          :aria-label="$t('next') || 'التالي'"
          @click.stop="next"
        >
          <b-icon icon="chevron-left"></b-icon>
        </button>
      </template>
    </div>

    <div v-if="items.length > 1" class="ann-slider-dots">
      <button
        v-for="(item, idx) in items"
        :key="'dot-' + (item.id || idx)"
        type="button"
        class="ann-dot"
        :class="{ active: idx === index }"
        :aria-label="'slide ' + (idx + 1)"
        @click="go(idx)"
      />
    </div>
  </div>
</template>

<script>
export default {
  name: "AnnouncementsSlider",
  props: {
    items: {
      type: Array,
      default: () => [],
    },
    intervalMs: {
      type: Number,
      default: 6500,
    },
  },
  data() {
    return {
      index: 0,
      timer: null,
    };
  },
  computed: {
    trackStyle() {
      return {
        transform: `translateX(-${this.index * 100}%)`,
      };
    },
  },
  watch: {
    items() {
      this.index = 0;
      this.restart();
    },
  },
  mounted() {
    this.restart();
  },
  beforeDestroy() {
    this.pause();
  },
  methods: {
    go(idx) {
      this.index = idx;
      this.restart();
    },
    next() {
      if (!this.items.length) return;
      this.index = (this.index + 1) % this.items.length;
      this.restart();
    },
    prev() {
      if (!this.items.length) return;
      this.index = (this.index - 1 + this.items.length) % this.items.length;
      this.restart();
    },
    pause() {
      if (this.timer) {
        clearInterval(this.timer);
        this.timer = null;
      }
    },
    resume() {
      this.restart();
    },
    restart() {
      this.pause();
      if (this.items.length > 1) {
        this.timer = setInterval(() => {
          this.index = (this.index + 1) % this.items.length;
        }, this.intervalMs);
      }
    },
    openLink(item) {
      if (!item?.linkUrl) return;
      window.open(item.linkUrl, "_blank", "noopener,noreferrer");
    },
  },
};
</script>

<style scoped>
.ann-slider {
  position: relative;
  margin-bottom: 1.25rem;
}

.ann-slider-viewport {
  position: relative;
  overflow: hidden;
  border-radius: 18px;
  background: #002536;
  box-shadow:
    0 1px 2px rgba(15, 23, 42, 0.04),
    0 12px 32px rgba(0, 37, 54, 0.12);
  min-height: 200px;
}

.ann-slider-track {
  display: flex;
  width: 100%;
  transition: transform 0.55s cubic-bezier(0.22, 1, 0.36, 1);
  direction: ltr;
}

.ann-slide {
  position: relative;
  flex: 0 0 100%;
  min-width: 100%;
  min-height: 220px;
  display: flex;
  align-items: flex-end;
  direction: rtl;
  text-align: start;
  overflow: hidden;
  isolation: isolate;
}

.ann-slide--clickable {
  cursor: pointer;
}

.ann-slide--no-image {
  background:
    radial-gradient(ellipse 80% 70% at 15% 20%, rgba(61, 180, 208, 0.28), transparent 55%),
    linear-gradient(135deg, #002536 0%, #0e7490 55%, #155e75 100%);
}

.ann-slide-media {
  position: absolute;
  inset: 0;
  z-index: 0;
  background-size: cover;
  background-position: center;
  transform: scale(1.02);
  transition: transform 6s ease;
}

.ann-slide--clickable:hover .ann-slide-media {
  transform: scale(1.06);
}

.ann-slide-shade {
  position: absolute;
  inset: 0;
  z-index: 1;
  background:
    linear-gradient(
      100deg,
      rgba(0, 20, 30, 0.88) 0%,
      rgba(0, 37, 54, 0.72) 42%,
      rgba(0, 37, 54, 0.28) 72%,
      rgba(0, 37, 54, 0.12) 100%
    );
  pointer-events: none;
}

.ann-slide--no-image .ann-slide-shade {
  background: transparent;
}

.ann-slide-content {
  position: relative;
  z-index: 2;
  width: min(560px, 100%);
  padding: 1.5rem 1.75rem 1.65rem;
  color: #f8fafc;
}

.ann-slide-badge {
  display: inline-flex;
  align-items: center;
  margin-bottom: 0.65rem;
  padding: 0.2rem 0.65rem;
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.02em;
  background: rgba(122, 215, 235, 0.2);
  border: 1px solid rgba(122, 215, 235, 0.35);
  color: #e0f2fe;
}

.ann-slide-title {
  margin: 0 0 0.45rem;
  font-size: clamp(1.2rem, 2.2vw, 1.65rem);
  font-weight: 800;
  line-height: 1.3;
  color: #fff;
  text-shadow: 0 1px 12px rgba(0, 0, 0, 0.25);
}

.ann-slide-text {
  margin: 0;
  max-width: 42ch;
  color: rgba(248, 250, 252, 0.88);
  font-size: 0.95rem;
  line-height: 1.65;
  white-space: pre-wrap;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.ann-slide-cta {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin-top: 0.95rem;
  padding: 0.45rem 0.9rem;
  border-radius: 999px;
  background: #fff;
  color: #002536;
  font-size: 0.85rem;
  font-weight: 700;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.ann-slide--clickable:hover .ann-slide-cta {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.18);
}

.ann-slide-cta-ico {
  font-size: 0.85rem;
}

.ann-nav {
  position: absolute;
  top: 50%;
  z-index: 3;
  transform: translateY(-50%);
  width: 38px;
  height: 38px;
  border: none;
  border-radius: 50%;
  display: grid;
  place-items: center;
  background: rgba(255, 255, 255, 0.92);
  color: #002536;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.16);
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.2s ease, transform 0.15s ease;
}

.ann-slider-viewport:hover .ann-nav,
.ann-slider-viewport:focus-within .ann-nav {
  opacity: 1;
}

.ann-nav:hover {
  transform: translateY(-50%) scale(1.05);
}

.ann-nav--prev {
  right: 0.75rem;
}

.ann-nav--next {
  left: 0.75rem;
}

.ann-slider-dots {
  display: flex;
  justify-content: center;
  gap: 0.4rem;
  margin-top: 0.75rem;
}

.ann-dot {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  border: none;
  background: #cbd5e1;
  padding: 0;
  cursor: pointer;
  transition: width 0.2s ease, background 0.2s ease;
}

.ann-dot.active {
  width: 22px;
  background: #0e7490;
}

@media (max-width: 768px) {
  .ann-slider-viewport,
  .ann-slide {
    min-height: 190px;
  }

  .ann-slide-content {
    width: 100%;
    padding: 1.15rem 1.1rem 1.25rem;
  }

  .ann-slide-shade {
    background: linear-gradient(
      180deg,
      rgba(0, 20, 30, 0.25) 0%,
      rgba(0, 37, 54, 0.55) 45%,
      rgba(0, 20, 30, 0.9) 100%
    );
  }

  .ann-nav {
    opacity: 0.85;
    width: 34px;
    height: 34px;
  }
}
</style>
