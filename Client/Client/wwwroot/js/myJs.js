(function () {
    'use strict'

    // Fetch all the forms we want to apply custom Bootstrap validation styles to
    var forms = document.querySelectorAll('.needs-validation')

    // Loop over them and prevent submission
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('input', function (event) {
                let buttons = document.getElementsByClassName("isWorking");
                let i;
                if (!form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();
                    for (i = 0; i < buttons.length; i++) {
                        buttons[i].disabled = true;
                    }
                }
                else {
                    for (i = 0; i < buttons.length; i++) {
                        buttons[i].disabled = false;
                    }
                }
                form.classList.add('was-validated')
            }, false)
        })
})();
$('textarea.resize').each(function () {
    this.setAttribute('style', 'height:' + (this.scrollHeight) + 'px;overflow-y:hidden;');
}).on('input', function () {
    this.style.height = 'auto';
    this.style.height = (this.scrollHeight) + 'px';
}).on('mousemove', function () {
    this.style.height = 'auto';
    this.style.height = (this.scrollHeight) + 'px';
}).on('keyup', function (event) {
    if (event.which === 9) {
        this.style.height = 'auto';
        this.style.height = (this.scrollHeight) + 'px';
    }
});
