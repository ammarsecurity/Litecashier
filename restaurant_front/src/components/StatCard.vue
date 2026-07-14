<template>
  <div
    class="app-overview-stat stat-card"
    :data-color="resolvedColor"
  >
    <span
      class="stat-icon app-overview-stat-icon"
      :class="'app-overview-stat-icon--' + resolvedColor"
    >
      <slot name="icon"></slot>
    </span>
    <div class="stat-content">
      <h3 class="stat-value app-overview-stat-value">{{ value }}</h3>
      <p class="stat-label app-overview-stat-label">{{ label }}</p>
    </div>
  </div>
</template>

<script>
export default {
  name: "StatCard",
  props: {
    value: {
      type: [String, Number],
      required: true,
    },
    label: {
      type: String,
      required: true,
    },
    color: {
      type: String,
      default: "primary",
    },
  },
  computed: {
    resolvedColor() {
      const allowed = ["primary", "success", "warning", "danger", "info"];
      return allowed.includes(this.color) ? this.color : "primary";
    },
  },
};
</script>
