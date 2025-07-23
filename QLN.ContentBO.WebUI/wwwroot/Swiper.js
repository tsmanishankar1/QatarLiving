let mainSwiperInstance;
let thumbsSwiperInstance;

window.initializeSwiperGallery = (dotNetRef, uniqueId) => {
    try {
        const container = document.getElementById(uniqueId);
        if (!container) {
            console.warn("Swiper container not found:", uniqueId);
            return;
        }

        // Destroy previous Swiper instances if they exist
        try {
            mainSwiperInstance?.destroy(true, true);
            mainSwiperInstance = null;

            thumbsSwiperInstance?.destroy(true, true);
            thumbsSwiperInstance = null;
        } catch (destroyErr) {
            console.error("Error destroying existing Swipers:", destroyErr);
        }

        const thumbSlides = container.querySelectorAll('.swiper-thumbs .swiper-slide');
        const thumbCount = thumbSlides.length;

        const thumbsWrapper = container.querySelector(".swiper-thumbs .swiper-wrapper");
        const thumbsPrev = container.querySelector(".thumbs-prev");
        const thumbsNext = container.querySelector(".thumbs-next");

        const thumbsConfig = {
            spaceBetween: 10,
            navigation: {
                nextEl: thumbsNext,
                prevEl: thumbsPrev,
            },
            breakpoints: {
                320: { slidesPerView: 2 },
                380: { slidesPerView: 2.6 },
                450: { slidesPerView: 3 },
                640: { slidesPerView: 4 },
                768: { slidesPerView: 5 },
                1024: { slidesPerView: 5 }
            }
        };

        if (thumbCount <= 5) {
            thumbsConfig.slidesPerView = thumbCount;
            thumbsConfig.loop = false;
            thumbsWrapper?.classList.add("few-thumbs");
            if (thumbsPrev) thumbsPrev.style.display = "none";
            if (thumbsNext) thumbsNext.style.display = "none";
        } else {
            thumbsConfig.slidesPerView = "auto";
        }

        // Initialize thumbnail swiper
        thumbsSwiperInstance = new Swiper(container.querySelector(".swiper-thumbs"), thumbsConfig);

        // Initialize main swiper
        mainSwiperInstance = new Swiper(container.querySelector(".swiper-main"), {
            spaceBetween: 10,
            pagination: {
                el: container.querySelector(".swiper-pagination"),
                clickable: true,
                dynamicBullets: true,
                dynamicMainBullets: 4,
            },
            thumbs: {
                swiper: thumbsSwiperInstance,
            },
            on: {
                slideChange: function () {
                    try {
                        dotNetRef?.invokeMethodAsync("UpdateActiveIndex", this.realIndex);
                    } catch (interopErr) {
                        console.error("Blazor interop error:", interopErr);
                    }
                }
            }
        });

        // Handle thumbnail clicks
        thumbsSwiperInstance.on("click", function (swiper) {
            try {
                if (typeof swiper.clickedIndex !== "undefined") {
                    mainSwiperInstance.slideTo(swiper.clickedIndex);
                }
            } catch (clickErr) {
                console.error("Error handling thumbnail click:", clickErr);
            }
        });

        // Manual next/prev
        thumbsNext?.addEventListener("click", () => {
            try {
                const nextIndex = Math.min(mainSwiperInstance.realIndex + 1, mainSwiperInstance.slides.length - 1);
                mainSwiperInstance.slideTo(nextIndex);
            } catch (navErr) {
                console.error("Error on next click:", navErr);
            }
        });

        thumbsPrev?.addEventListener("click", () => {
            try {
                const prevIndex = Math.max(mainSwiperInstance.realIndex - 1, 0);
                mainSwiperInstance.slideTo(prevIndex);
            } catch (navErr) {
                console.error("Error on prev click:", navErr);
            }
        });

        // Allow external slide control
        window.goToSlide = (index) => {
            try {
                if (mainSwiperInstance && typeof index === "number") {
                    mainSwiperInstance.slideTo(index);
                }
            } catch (goToErr) {
                console.error("goToSlide error:", goToErr);
            }
        };
    } catch (err) {
        console.error("initializeSwiperGallery failed:", err);
    }
};


let mainImagePreviewSwiperInstance;
let thumbsImagePreviewSwiperInstance;

window.initializeSwiperImagePreview = (dotNetRef, uniqueId) => {
    try {
        const container = document.getElementById(uniqueId);
        if (!container) {
            console.warn("Swiper Image Preview container not found:", uniqueId);
            return;
        }

        // Destroy existing Swipers safely
        try {
            mainImagePreviewSwiperInstance?.destroy(true, true);
            thumbsImagePreviewSwiperInstance?.destroy(true, true);
            mainImagePreviewSwiperInstance = null;
            thumbsImagePreviewSwiperInstance = null;
        } catch (destroyErr) {
            console.error("Failed to destroy Swiper instances:", destroyErr);
        }

        const thumbSlides = container.querySelectorAll('.swiper-thumbs-preview .swiper-slide');
        const thumbCount = thumbSlides.length;

        const thumbsWrapper = container.querySelector(".swiper-thumbs-preview .swiper-wrapper");
        const thumbsPrev = container.querySelector(".thumbs-prev-preview");
        const thumbsNext = container.querySelector(".thumbs-next-preview");

        const thumbsConfig = {
            spaceBetween: 10,
            navigation: {
                nextEl: thumbsNext,
                prevEl: thumbsPrev,
            },
            breakpoints: {
                380: { slidesPerView: 2.6 },
                450: { slidesPerView: 3 },
                640: { slidesPerView: 4 },
                768: { slidesPerView: 5 },
                1024: { slidesPerView: 5 },
            }
        };

        if (thumbCount <= 5) {
            thumbsConfig.slidesPerView = thumbCount;
            thumbsConfig.loop = false;

            thumbsWrapper?.classList.add("few-thumbs");

            if (thumbsPrev) thumbsPrev.style.display = "none";
            if (thumbsNext) thumbsNext.style.display = "none";
        } else {
            thumbsConfig.slidesPerView = "auto";
        }

        // Initialize thumbnails swiper
        thumbsImagePreviewSwiperInstance = new Swiper(container.querySelector(".swiper-thumbs-preview"), thumbsConfig);

        // Initialize main swiper
        mainImagePreviewSwiperInstance = new Swiper(container.querySelector(".swiper-main-preview"), {
            spaceBetween: 10,
            pagination: {
                el: container.querySelector(".swiper-pagination-preview"),
                clickable: true,
                dynamicBullets: true,
                dynamicMainBullets: 4,
            },
            thumbs: {
                swiper: thumbsImagePreviewSwiperInstance,
            },
            on: {
                slideChange: function () {
                    try {
                        dotNetRef?.invokeMethodAsync("UpdateActiveIndex", this.realIndex);
                    } catch (interopErr) {
                        console.error("Blazor invokeMethodAsync error:", interopErr);
                    }
                }
            }
        });

        // Handle thumbnail click
        thumbsImagePreviewSwiperInstance.on('click', function (swiper) {
            try {
                if (typeof swiper.clickedIndex !== 'undefined') {
                    mainImagePreviewSwiperInstance.slideTo(swiper.clickedIndex);
                }
            } catch (clickErr) {
                console.error("Error on thumbnail click:", clickErr);
            }
        });

        // Manual navigation next/prev
        thumbsNext?.addEventListener("click", () => {
            try {
                const currentIndex = mainImagePreviewSwiperInstance.realIndex;
                const nextIndex = Math.min(currentIndex + 1, mainImagePreviewSwiperInstance.slides.length - 1);
                mainImagePreviewSwiperInstance.slideTo(nextIndex);
            } catch (nextErr) {
                console.error("Error navigating next:", nextErr);
            }
        });

        thumbsPrev?.addEventListener("click", () => {
            try {
                const currentIndex = mainImagePreviewSwiperInstance.realIndex;
                const prevIndex = Math.max(currentIndex - 1, 0);
                mainImagePreviewSwiperInstance.slideTo(prevIndex);
            } catch (prevErr) {
                console.error("Error navigating prev:", prevErr);
            }
        });

        // External control
        window.goToImagePreviewSlide = (index) => {
            try {
                if (mainImagePreviewSwiperInstance && typeof index === 'number') {
                    mainImagePreviewSwiperInstance.slideTo(index);
                }
            } catch (controlErr) {
                console.error("goToImagePreviewSlide error:", controlErr);
            }
        };
    } catch (err) {
        console.error("initializeSwiperImagePreview error:", err);
    }
};


   window.getDropdownSpace = (element) => {
    if (!element) return { spaceAbove: 0, spaceBelow: 9999 };

    const rect = element.getBoundingClientRect();
    const viewportHeight = window.innerHeight;

    return {
        spaceAbove: rect.top,
        spaceBelow: viewportHeight - rect.bottom
    };
};
