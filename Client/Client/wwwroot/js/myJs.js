//$('textarea.resize').each(function () {
//    this.setAttribute('style', 'height:' + (this.scrollHeight) + 'px;overflow-y:hidden;');
//}).on('input', function () {
//    this.style.height = 'auto';
//    this.style.height = (this.scrollHeight) + 'px';
//});
(function () {
    'use strict'

    // Fetch all the forms we want to apply custom Bootstrap validation styles to
    var forms = document.querySelectorAll('.needs-validation')

    // Loop over them and prevent submission
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (!form.checkValidity()) {
                    event.preventDefault()
                    event.stopPropagation()
                }

                form.classList.add('was-validated')
            }, false)
        })
})()
