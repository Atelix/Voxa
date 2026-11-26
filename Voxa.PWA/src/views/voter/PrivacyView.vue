<template>
	<div class="px-4 pt-4">
		<v-btn variant="text" prepend-icon="mdi-arrow-left" class="mb-2 px-0" @click="$router.back()">{{
			$t("privacy.back")
		}}</v-btn>
		<h1 class="voxa-heading text-primary mb-2" style="font-size: 1.8rem">{{ $t("privacy.title") }}</h1>
		<p class="text-body-large mb-6">{{ $t("privacy.lead") }}</p>
		<div class="d-flex flex-column ga-4">
			<div v-for="n in 5" :key="n" class="d-flex ga-4">
				<span
					class="voxa-heading bg-secondary rounded-circle d-flex align-center justify-center flex-shrink-0"
					style="width: 36px; height: 36px"
					>{{ n }}</span
				>
				<div>
					<p class="font-weight-bold">{{ $t(`privacy.points.${n}.title`) }}</p>
					<p class="text-body-medium text-medium-emphasis">{{ $t(`privacy.points.${n}.text`) }}</p>
				</div>
			</div>
		</div>
		<p class="text-body-medium mt-6">{{ $t("privacy.paper") }}</p>
	</div>
</template>
