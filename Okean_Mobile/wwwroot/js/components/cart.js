export class Cart {
    constructor() {
        this.initializeToast();
        this.setupEventListeners();
    }

    initializeToast() {
        this.cartToast = new bootstrap.Toast(document.getElementById('cartToast'), {
            autohide: true,
            delay: 3000
        });
    }

    setupEventListeners() {
        $('.add-to-cart-form').submit((e) => this.handleAddToCart(e));
    }

    handleAddToCart(e) {
        e.preventDefault();
        
        const form = $(e.target);
        const formData = {
            productId: form.find('input[name="productId"]').val(),
            quantity: form.find('input[name="quantity"]').val(),
            __RequestVerificationToken: form.find('input[name="__RequestVerificationToken"]').val()
        };

        $.ajax({
            url: '/Cart/AddToCart',
            type: 'POST',
            data: formData,
            success: (response) => {
                if (response.success) {
                    // Update cart count in navbar
                    $('.cart-count').text(response.cartCount);
                    // Show toast notification
                    this.cartToast.show();
                } else {
                    // Show error message
                    alert(response.message);
                }
            },
            error: (xhr, status, error) => {
                console.error('Error:', error);
                console.error('Status:', status);
                console.error('Response:', xhr.responseText);
                alert('Có lỗi xảy ra khi thêm vào giỏ hàng. Vui lòng thử lại sau.');
            }
        });
    }
} 