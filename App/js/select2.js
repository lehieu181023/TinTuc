select2 = function () {
    $('.select2zz').select2({
        placeholder: "Chọn giá trị...",
        allowClear: true,
        width: '100%'
    });
}
$('.select2DM').each(function () {
    const $select = $(this);
    const $modal = $select.closest('.modal'); // tìm phần tử cha là modal

    $select.select2({
        dropdownParent: $modal.length ? $modal : $(document.body), // nếu có modal thì đặt, không thì thôi
        placeholder: 'Tìm kiếm danh mục...',
        minimumInputLength: 2,
        allowClear: true,
        ajax: {
            url: '/DmTinTuc/GetDmSelect',
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return {
                    searchTerm: params.term,
                    page: params.page || 1
                };
            },
            processResults: function (data, params) {
                params.page = params.page || 1;
                return {
                    results: data.items,
                    pagination: {
                        more: data.hasMore
                    }
                };
            }
        }
    });
});


select2();

