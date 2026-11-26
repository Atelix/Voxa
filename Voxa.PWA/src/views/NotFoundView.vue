<template>
	<v-container class="py-12 text-center" max-width="560">
		<v-icon icon="mdi-map-marker-question-outline" size="64" class="mb-4 text-medium-emphasis" />
		<h1 class="text-headline-small mb-2">{{ $t("notFound.title") }}</h1>
		<p class="text-body-large text-medium-emphasis mb-6">{{ $t("notFound.text") }}</p>
		<v-btn color="primary" :to="{ name: 'items' }">{{ $t("notFound.home") }}</v-btn>
	</v-container>
</template>
