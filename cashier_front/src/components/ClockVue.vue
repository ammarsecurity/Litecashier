<template>
    <div>
        <div id="clock" class="align-middle">
            <div class="date bold_text">{{ date }}</div>
            <div class="time bold_text">{{ time }}</div>
        </div>
    </div>
</template>

<script>

export default {
    name: "ClockVue",
    data() {
        return {
            time: ``,
            date: ``,
        };
    },
    mounted() {
        this.updateTime();
        this.timerID = setInterval(this.updateTime, 1000);
    },
    beforeDestroy() {
        clearInterval(this.timerID);
    },
    computed: {
        formattedNumber() {
            return this.totaPrice.toLocaleString()
        }
    },
    methods: {
        updateTime() {
            var cd = new Date();
            this.time =
                this.zeroPadding(cd.getHours(), 2) +
                ':' +
                this.zeroPadding(cd.getMinutes(), 2) +
                ':' +
                this.zeroPadding(cd.getSeconds(), 2);
            this.date =
                this.zeroPadding(cd.getFullYear(), 4) +
                '-' +
                this.zeroPadding(cd.getMonth() + 1, 2) +
                '-' +
                this.zeroPadding(cd.getDate(), 2) +
                ' '

        },
        zeroPadding(num, digit) {
            var zero = '';
            for (var i = 0; i < digit; i++) {
                zero += '0';
            }
            return (zero + num).slice(-digit);
        }
    }

};
</script>
