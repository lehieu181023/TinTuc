var isLoadingDanhMuc = false;

LoadDanhMuc = function (keysearch, reload) {
    if (isLoadingDanhMuc) return; 

    isLoadingDanhMuc = true;
    BlockUI();
    if (keysearch === undefined) keysearch = "";
    if (reload === undefined) reload = false;

    var index = 0;
    var $indexdm = $('#indexdm');
    index = $indexdm.val();
    if (index === undefined || index === null || index === "") {
        index = 0;
    }

    $.ajax({
        url: `/Home/ListDm`,
        type: "GET",
        data: { keysearch: keysearch, indexdm: index },
        success: function (response) {
            if (response.success === false) {
                showToast(response.message);
            } else {
                if (reload) {
                    $('#listDanhMuc').html(response);
                } else {
                    $('#listDanhMuc').append(response);
                    $indexdm.val(parseInt(index) + 50);
                }
            }
        },
        error: function (xhr) {
            if (xhr.status === 401) {
                showToast("Bạn không có quyền truy cập! Vui lòng đăng nhập.");
            } else if (xhr.status === 403) {
                showToast("Bạn không có quyền thực hiện thao tác này!");
            } else {
                showToast("Không thể tải dữ liệu!");
            }
        },
        complete: function () {
            UnBlockUI();
            isLoadingDanhMuc = false;
        }
    });
}

LoadTinTuc = function () {
    BlockUI();
    var dataRequest = {};

    $("#search").find("input, select, textarea").each(function () {
        var $el = $(this);
        var name = $el.attr("name") || $el.attr("id");
        if (!name || $el.prop("disabled")) return;

        var rawValue = $el.val();

        if (Array.isArray(rawValue)) {
            // select[multiple] => nối thành chuỗi "1,2,3"
            if (rawValue.length > 0) {
                dataRequest[name] = rawValue.join(",");
            }
        } else {
            var value = (rawValue ?? "").toString().trim();
            if (value !== "") {
                dataRequest[name] = value;
            }
        }
    });
    $.ajax({
        url: `/Home/ListdataTinTuc`,
        type: "GET",
        data: dataRequest,
        success: function (response) {
            if (response.success = false) {
                showToast(response.message);
            }
            else {
                $('#lstTinTuc').html(response);
            }
            UnBlockUI();
        },
        error: function (xhr) {
            UnBlockUI();
            if (xhr.status === 401) {
                showToast("Bạn không có quyền truy cập! Vui lòng đăng nhập.");
            } else if (xhr.status === 403) {
                showToast("Bạn không có quyền thực hiện thao tác này!");
            } else {
                showToast("Không thể tải dữ liệu!");
            }
        }
    });
}

$(document).ready(function () {
    $('#btnTaoDuLieu').click(function () {
        BlockUI();
        var $btn = $(this);
        var originalText = $btn.text();

        // Cập nhật UI
        $btn.text('Đang tạo...').prop('disabled', true);

        $.ajax({
            url: '/Home/GenDuLieu',
            method: 'POST',
            success: function (res) {
                // Cập nhật UI
                $btn.text(' tạo...').prop('disabled', true);
            },
            error: function () {
                UnBlockUI();
                alert("Gửi yêu cầu thất bại!");
                $btn.text(originalText).prop('disabled', false);
            }
        });
    });
});



function ClickDM(el) {
    var $button = $(el);
    var IdDM = $button.data('category');
    if (IdDM === undefined) IdDM = 0;

    var divchild = $('#DivChild-' + IdDM);
    $('#IdDm').val(IdDM);
    LoadTinTuc();
    BlockUI();
    $.ajax({
        url: `/Home/ListDmChild`,
        type: "GET",
        data: { IdDm: IdDM },
        success: function (response) {
            debugger;
            if (response.success === false) {
                showToast(response.message);
            } else {
                $(divchild).html(response);

                var $children = $button.siblings('.category-children');
                if ($children.length > 0) {
                    $children.slideToggle(300);
                    $button.toggleClass('expanded');
                }

                $('.category-toggle').removeClass('active');
                $button.addClass('active');

                var categoryData = $button.data('category');
                console.log('Selected category:', categoryData);
            }
            UnBlockUI();
        },
        error: function (xhr) {
            UnBlockUI();
            if (xhr.status === 401) {
                showToast("Bạn không có quyền truy cập! Vui lòng đăng nhập.");
            } else if (xhr.status === 403) {
                showToast("Bạn không có quyền thực hiện thao tác này!");
            } else {
                showToast("Không thể tải dữ liệu!");
            }
        }
    });
}
closeAll = function () {

         $('.category-children').slideUp(300);
         $('.category-toggle').removeClass('expanded');
         $('.category-toggle').removeClass('active');
    
};
$('#listDanhMuc').on('scroll', function () {
    let $container = $(this);
    if ($container.scrollTop() + $container.innerHeight() >= $container[0].scrollHeight - 30) {
        let keysearch = $('input[placeholder="Tìm kiếm danh mục..."]').val() || "";
        LoadDanhMuc(keysearch, false);
    }
});

LoadDanhMuc();
LoadTinTuc();