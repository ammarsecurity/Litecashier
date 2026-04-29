<template>
    <div class="stat-card">
        <div class="stat-icon">
            <slot name="icon"></slot>
        </div>
        <div class="stat-content">
            <h3 class="stat-value">{{ value }}</h3>
            <p class="stat-label">{{ label }}</p>
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
    },
};
</script>

