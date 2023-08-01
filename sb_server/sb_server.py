import base64
import os.path
import time
from io import BytesIO

import requests
from tqdm import tqdm
from flask import Flask, request, send_file
from PIL import Image
import io
import cv2
import numpy as np

app = Flask(__name__)

def pil2cv(image):
    res = np.array(image)
    res = cv2.cvtColor(res, cv2.COLOR_RGB2BGR)
    return res

def preprocess_mask(mask):
    mask = np.array(mask)
    mask = cv2.cvtColor(mask, cv2.COLOR_RGB2GRAY)
    ret, mask = cv2.threshold(mask, 10, 255, cv2.THRESH_BINARY)


    # cv2.imshow("Mask0", mask)
    # mask = cv2.GaussianBlur(mask, (41, 41), 0)
    # ret, mask = cv2.threshold(mask, 10, 255, cv2.THRESH_BINARY)
    # cv2.imshow("Mask1", mask)
    # # small = cv2.resize(mask, fx = 0.1, fy = 0.1, dsize = None)
    #
    #
    # cv2.waitKey(0)
    mask = Image.fromarray(cv2.cvtColor(mask, cv2.COLOR_GRAY2RGB))
    return mask

# mask = Image.open("mask1.png")
# mask = preprocess_mask(mask)
# mask.show()
# cv2.imshow("s", mask)
# cv2.waitKey(0)

def test():
    mask = Image.open("mask1.png")
    image = Image.open("test1.png")

    # mask = Image.open("results/src/mask_0001.jpg")
    # image = Image.open("results/src/image_0001.jpg")
    mask = preprocess_mask(mask)

    mask.save("res_mask.png")
    process_sb(image, mask)


@app.route('/ping', methods=['GET'])
def ping():
    print("ping")
    return "pong"
@app.route('/process', methods=['POST'])
def images():
    print("Received")
    # Get images from request
    try:
        # time.sleep(10)
        # image = Image.open("test1.png")
        # img_io = io.BytesIO()
        # image.save(img_io, 'JPEG', quality=70)
        # img_io.seek(0)
        # return send_file(img_io, mimetype='image/jpeg')

        image1 = Image.open(io.BytesIO(request.files['image1'].read()))
        image2 = Image.open(io.BytesIO(request.files['image2'].read()))

        image1.save("test1.png")
        image2.save("mask1.png")

        mask = preprocess_mask(image2)
        mask.save("res_mask.png")

        image = process_sb(image1, mask)
        # Process images here to create new image
        # image = Image.blend(image1, image2, 0.5)
        # image.show()
        # Convert new image to byte array
        # img_byte_arr = io.BytesIO()
        # new_image.save(img_byte_arr, format='PNG')
        # img_byte_arr = img_byte_arr.getvalue()
        # return img_byte_arr
        img_io = io.BytesIO()
        image.save(img_io, 'JPEG', quality=70)
        img_io.seek(0)
        return send_file(img_io, mimetype='image/jpeg')
    except:
        print("Error")
        return "Err"


def image2base64(image):
    buffered = BytesIO()
    image.save(buffered, format="PNG")


    array = np.asarray(bytearray(buffered.getvalue()), dtype=np.uint8)
    image = cv2.imdecode(array, cv2.IMREAD_COLOR)



    return base64.b64encode(buffered.getvalue()).decode('utf-8'), image

def process_sb(image, mask):
    url = 'http://192.168.31.221:7860/sdapi/v1/img2img'
    url = 'http://10.8.0.214:7860/sdapi/v1/img2img'
    url = 'http://mlserver.nordclan:7861/sdapi/v1/img2img'
    # url = 'http://127.0.0.1:7860/sdapi/v1/img2img'
    image_str, image_cv = image2base64(image)
    mask_str, mask_cv =  image2base64(mask)

    # cv2.imshow("Image", image_cv)
    # cv2.imshow("mask", mask_cv)
    # cv2.imwrite("image.png", image_cv)
    # cv2.imwrite("mask.png", image_cv)
    # cv2.waitKey(1)

    w, h = image.size

    if w > h:
        w = int(w/h * 512)
        h = 512
    else:
        h = int(h/w * 512)
        w = 512
    w, h= (w//4)*4, (h//4)*4
    print(w, h)


    params = {
        "init_images": [image_str],
        "outpath_samples": "outputs/img2img-imag",
        "outpath_grids": "outputs/img2img-grid",
        "prompt": "outfit, fashion, high quality, adorable, functional and elegant look, absolutely outstanding image",
        "negative_prompt": "nude, naked",
        "seed": -1,
        "seed_resize_from_h": 0,
        "seed_resize_from_w": 0,
        "sampler_name": "DPM++ SDE Karras",
        #"sampler_name": "Euler a",
        "batch_size": 1,
        "n_iter": 1,
        "steps": 8,
        "cfg_scale": 7,
        "width": w,
        "height": h,
        "restore_faces": False,
        "tiling": False,
        "extra_generation_params": "{'Mask blur': 4}",

        # "negative_prompt": "string",

        "denoising_strength": 0.6,
        "resize_mode": 0,
        "image_cfg_scale": 0,
        "mask": mask_str,
        "mask_blur": 4,
        "inpainting_fill": 1, # onlymasked
        "inpaint_full_res": 1,
        "inpaint_full_res_padding": 32,
        "inpainting_mask_invert": 0,
        "initial_noise_multiplier": 1,
        # "send_images": True,
        # "save_images": True,

        # "do_not_save_samples": True,
        # "do_not_save_grid": True,
        #
        "alwayson_scripts": {
            "controlnet": {
                "args": [
                    {
                        "enabled": True,
                        # "image": image_str,
                        # "mask": mask_str,
                        "module": "depth_leres",
                        # "model": "control_sd15_depth [fef5e48e]",
                        "model": "control_v11f1p_sd15_depth [cfd03158]",

                        # "module": "openpose_full",
                        # "model": "control_sd15_openpose [fef5e48e]",

                        "weight": 1,
                        "resize_mode": "Just Resize",
                        "lowvram": False,
                        "processor_res": 512,
                        "threshold_a": 0,
                        "threshold_b": 0,
                        "guidance": 1,
                        "guidance_start": 0,
                        "guidance_end": 1,
                        "guess_mode": False,
                    },
                ]
            }
        }

    }
    # img = mask_cv
    #
    # img[:,:,:] = 255
    # img[:, :, 0] = 0
    # img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    # im_pil = Image.fromarray(img)
    # return im_pil

    print("Send request to ", url)
    response = requests.post(url, json=params)  # , datajson=params)
    print(response)
    response_data = response.json()



    # generated_image_data = response_data['images'][0]
    result = None
    for k, generated_image_data in enumerate(response_data['images']):
        generated_image = Image.open(BytesIO(base64.b64decode(generated_image_data)))
        generated_image.save('res.png')
        for i in range(1, 1000):
            name = f'results/res_{i:04d}.jpg'
            if not os.path.exists(name):
                generated_image.save(name)
                res = pil2cv(generated_image)

                if k == 0:
                    cv2.imshow("image", res)
                    cv2.waitKey(1)
                    result = generated_image

                image.save(f'results/src/image_{i:04d}.jpg')
                mask.save(f'results/src/mask_{i:04d}.jpg')

                try:
                    full = np.hstack((pil2cv(image), res))
                    cv2.imwrite(f'results/res_full_{i:04d}.jpg', full)
                except:
                    #TODO
                    #depth
                    pass
                break
    # generated_image.show()
    return result

def process_batch():
    folder = 'results/srccopy'
    files = [(n, os.path.join(folder, n)) for n in os.listdir(folder) if 'image' in n]

    for f, dir in tqdm(files):
        mask_name = dir.replace('image', 'mask')
        if not os.path.exists(mask_name):
            print("mask not exist", mask_name)
            continue

        image = Image.open(dir)
        mask = Image.open(mask_name)
        mask = preprocess_mask(mask)
        process_sb(image, mask)



# process_batch()
if __name__ == '__main__':
    # test()
    app.run(host = "0.0.0.0", port = 5002)