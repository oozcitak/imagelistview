$(document).ready(function () {

    var heroImageCaptions = ['Thumbnail view mode', 'Gallery view mode', 'Details view mode',
        'Pane view mode', 'Tiles renderer', 'XP renderer', 'Noir renderer'];

    $('.demo-images').slick({
        slidesToShow: 1,
        slidesToScroll: 1,
        arrows: true,
        fade: true,
        dots: false,
        cssEase: 'linear',
    });

    $('.demo-images').on('beforeChange', function (event, slick, currentSlide, nextSlide) {
        $('.demo-images-caption').text(heroImageCaptions[nextSlide]);
    });

    $('.demo-images-caption').text(heroImageCaptions[0]);

});
